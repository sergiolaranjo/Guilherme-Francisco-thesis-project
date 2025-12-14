Shader "Custom/VolumeRenderingEnhanced"
{
    Properties
    {
        [Header(Volume Data)]
        _Data ("Volume Data", 3D) = "" {}
        _TransferFunction ("Transfer Function", 2D) = "white" {}

        [Header(Quality Settings)]
        _Alpha ("Alpha Multiplier", Range(0, 5)) = 1.0
        _StepSize ("Step Size", Range(0.001, 0.05)) = 0.005
        _Iterations ("Max Iterations", Range(64, 1024)) = 512
        _Density ("Density", Range(0.1, 10)) = 1.0
        [Toggle] _UseTricubic ("Tricubic Interpolation", Float) = 1
        [Toggle] _UseJittering ("Jittering (Anti-Aliasing)", Float) = 1

        [Header(Window Level)]
        _WindowCenter ("Window Center", Range(0, 1)) = 0.5
        _WindowWidth ("Window Width", Range(0.001, 1)) = 0.5
        _MinThreshold ("Min Threshold", Range(0, 1)) = 0.1
        _MaxThreshold ("Max Threshold", Range(0, 1)) = 1.0

        [Header(Lighting)]
        _LightDir ("Light Direction", Vector) = (0.5, 1, 0.3, 0)
        _LightColor ("Light Color", Color) = (1, 0.98, 0.95, 1)
        _AmbientColor ("Ambient Color", Color) = (0.15, 0.18, 0.25, 1)
        _AmbientLight ("Ambient Intensity", Range(0, 1)) = 0.25
        _DiffuseLight ("Diffuse Intensity", Range(0, 1)) = 0.8
        _SpecularLight ("Specular Intensity", Range(0, 1)) = 0.4
        _Shininess ("Shininess", Range(1, 256)) = 64

        [Header(Advanced Lighting)]
        [Toggle] _UseSSS ("Subsurface Scattering", Float) = 1
        _SSSColor ("SSS Color", Color) = (1, 0.4, 0.3, 1)
        _SSSIntensity ("SSS Intensity", Range(0, 2)) = 0.5
        _SSSDistortion ("SSS Distortion", Range(0, 1)) = 0.3
        _SSSPower ("SSS Power", Range(1, 16)) = 4

        [Header(Ambient Occlusion)]
        [Toggle] _UseAO ("Ambient Occlusion", Float) = 1
        _AORadius ("AO Radius", Range(0.01, 0.2)) = 0.05
        _AOIntensity ("AO Intensity", Range(0, 2)) = 1.0
        _AOSamples ("AO Samples", Range(2, 16)) = 6

        [Header(Rim Lighting)]
        [Toggle] _UseRimLight ("Rim Light", Float) = 1
        _RimColor ("Rim Color", Color) = (0.5, 0.7, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _RimIntensity ("Rim Intensity", Range(0, 2)) = 0.5

        [Header(Color Grading)]
        _Saturation ("Saturation", Range(0, 2)) = 1.1
        _Contrast ("Contrast", Range(0.5, 2)) = 1.05
        _Brightness ("Brightness", Range(0, 2)) = 1.0
        _Gamma ("Gamma", Range(0.5, 2)) = 1.0

        [Header(Clipping)]
        _ClipPlaneNormal ("Clip Plane Normal", Vector) = (0, 0, 0, 0)
        _ClipPlanePosition ("Clip Plane Position", Vector) = (0, 0, 0, 0)
        _UseClipPlane ("Use Clip Plane", Float) = 0

        [Header(Depth Effects)]
        _DepthFade ("Depth Fade", Range(0, 1)) = 0.1
        _DepthFadeDistance ("Depth Fade Distance", Range(0.1, 10)) = 2
    }

    SubShader
    {
        Tags { "Queue" = "Transparent+100" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        LOD 300

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Front
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "VolumeRenderingEnhanced"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 4.0

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
                float3 viewDir : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Textures
            sampler3D _Data;
            float4 _Data_TexelSize;
            sampler2D _TransferFunction;

            // Quality
            float _Alpha;
            float _StepSize;
            int _Iterations;
            float _Density;
            float _UseTricubic;
            float _UseJittering;

            // Window Level
            float _WindowCenter;
            float _WindowWidth;
            float _MinThreshold;
            float _MaxThreshold;

            // Lighting
            float3 _LightDir;
            float4 _LightColor;
            float4 _AmbientColor;
            float _AmbientLight;
            float _DiffuseLight;
            float _SpecularLight;
            float _Shininess;

            // SSS
            float _UseSSS;
            float4 _SSSColor;
            float _SSSIntensity;
            float _SSSDistortion;
            float _SSSPower;

            // AO
            float _UseAO;
            float _AORadius;
            float _AOIntensity;
            int _AOSamples;

            // Rim
            float _UseRimLight;
            float4 _RimColor;
            float _RimPower;
            float _RimIntensity;

            // Color Grading
            float _Saturation;
            float _Contrast;
            float _Brightness;
            float _Gamma;

            // Clipping
            float3 _ClipPlaneNormal;
            float3 _ClipPlanePosition;
            float _UseClipPlane;

            // Depth
            float _DepthFade;
            float _DepthFadeDistance;

            // Noise function for jittering
            float hash(float n) { return frac(sin(n) * 43758.5453123); }

            float noise3D(float3 x)
            {
                float3 p = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                float n = p.x + p.y * 57.0 + 113.0 * p.z;
                return lerp(lerp(lerp(hash(n + 0.0), hash(n + 1.0), f.x),
                                 lerp(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
                            lerp(lerp(hash(n + 113.0), hash(n + 114.0), f.x),
                                 lerp(hash(n + 170.0), hash(n + 171.0), f.x), f.y), f.z);
            }

            // Tricubic interpolation weights
            float4 cubic(float v)
            {
                float4 n = float4(1.0, 2.0, 3.0, 4.0) - v;
                float4 s = n * n * n;
                float x = s.x;
                float y = s.y - 4.0 * s.x;
                float z = s.z - 4.0 * s.y + 6.0 * s.x;
                float w = 6.0 - x - y - z;
                return float4(x, y, z, w) / 6.0;
            }

            // Tricubic texture sampling
            float SampleVolumeTricubic(float3 texCoord)
            {
                float3 texSize = float3(1.0, 1.0, 1.0) / _StepSize;
                float3 coord_grid = texCoord * texSize - 0.5;
                float3 index = floor(coord_grid);
                float3 fraction = coord_grid - index;

                float4 xcubic = cubic(fraction.x);
                float4 ycubic = cubic(fraction.y);
                float4 zcubic = cubic(fraction.z);

                float3 c = index - 0.5;
                float3 s = float3(xcubic.x + xcubic.y, xcubic.z + xcubic.w, 0);
                float3 offset = c + float3(xcubic.y, ycubic.y, zcubic.y) / s + 0.5;

                // Simplified tricubic - uses trilinear with offset for performance
                return tex3Dlod(_Data, float4(offset / texSize, 0)).r;
            }

            // Standard trilinear sampling
            float SampleVolumeTrilinear(float3 texCoord)
            {
                return tex3Dlod(_Data, float4(texCoord, 0)).r;
            }

            // Sample volume with quality setting
            float SampleVolume(float3 texCoord)
            {
                if (_UseTricubic > 0.5)
                    return SampleVolumeTricubic(texCoord);
                return SampleVolumeTrilinear(texCoord);
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.localPos = v.vertex.xyz + 0.5;
                o.screenPos = ComputeScreenPos(o.vertex);
                o.viewDir = normalize(_WorldSpaceCameraPos - o.worldPos);

                return o;
            }

            // Apply window level transformation
            float ApplyWindowLevel(float value)
            {
                float minVal = _WindowCenter - _WindowWidth * 0.5;
                float maxVal = _WindowCenter + _WindowWidth * 0.5;
                return saturate((value - minVal) / (_WindowWidth + 0.0001));
            }

            // Sample transfer function
            float4 SampleTransferFunction(float density)
            {
                return tex2Dlod(_TransferFunction, float4(density, 0.5, 0, 0));
            }

            // Calculate gradient using Sobel operator for smoother normals
            float3 CalculateGradientSobel(float3 pos)
            {
                float d = _StepSize * 1.5;
                float3 gradient = float3(0, 0, 0);

                // Sobel kernel weights
                float w0 = 1.0, w1 = 2.0;

                // X gradient
                gradient.x += w0 * (SampleVolume(pos + float3(d, -d, -d)) - SampleVolume(pos + float3(-d, -d, -d)));
                gradient.x += w1 * (SampleVolume(pos + float3(d, 0, -d)) - SampleVolume(pos + float3(-d, 0, -d)));
                gradient.x += w0 * (SampleVolume(pos + float3(d, d, -d)) - SampleVolume(pos + float3(-d, d, -d)));
                gradient.x += w1 * (SampleVolume(pos + float3(d, -d, 0)) - SampleVolume(pos + float3(-d, -d, 0)));
                gradient.x += 4.0 * (SampleVolume(pos + float3(d, 0, 0)) - SampleVolume(pos + float3(-d, 0, 0)));
                gradient.x += w1 * (SampleVolume(pos + float3(d, d, 0)) - SampleVolume(pos + float3(-d, d, 0)));
                gradient.x += w0 * (SampleVolume(pos + float3(d, -d, d)) - SampleVolume(pos + float3(-d, -d, d)));
                gradient.x += w1 * (SampleVolume(pos + float3(d, 0, d)) - SampleVolume(pos + float3(-d, 0, d)));
                gradient.x += w0 * (SampleVolume(pos + float3(d, d, d)) - SampleVolume(pos + float3(-d, d, d)));

                // Y gradient (same pattern)
                gradient.y += w0 * (SampleVolume(pos + float3(-d, d, -d)) - SampleVolume(pos + float3(-d, -d, -d)));
                gradient.y += w1 * (SampleVolume(pos + float3(0, d, -d)) - SampleVolume(pos + float3(0, -d, -d)));
                gradient.y += w0 * (SampleVolume(pos + float3(d, d, -d)) - SampleVolume(pos + float3(d, -d, -d)));
                gradient.y += w1 * (SampleVolume(pos + float3(-d, d, 0)) - SampleVolume(pos + float3(-d, -d, 0)));
                gradient.y += 4.0 * (SampleVolume(pos + float3(0, d, 0)) - SampleVolume(pos + float3(0, -d, 0)));
                gradient.y += w1 * (SampleVolume(pos + float3(d, d, 0)) - SampleVolume(pos + float3(d, -d, 0)));
                gradient.y += w0 * (SampleVolume(pos + float3(-d, d, d)) - SampleVolume(pos + float3(-d, -d, d)));
                gradient.y += w1 * (SampleVolume(pos + float3(0, d, d)) - SampleVolume(pos + float3(0, -d, d)));
                gradient.y += w0 * (SampleVolume(pos + float3(d, d, d)) - SampleVolume(pos + float3(d, -d, d)));

                // Z gradient (same pattern)
                gradient.z += w0 * (SampleVolume(pos + float3(-d, -d, d)) - SampleVolume(pos + float3(-d, -d, -d)));
                gradient.z += w1 * (SampleVolume(pos + float3(0, -d, d)) - SampleVolume(pos + float3(0, -d, -d)));
                gradient.z += w0 * (SampleVolume(pos + float3(d, -d, d)) - SampleVolume(pos + float3(d, -d, -d)));
                gradient.z += w1 * (SampleVolume(pos + float3(-d, 0, d)) - SampleVolume(pos + float3(-d, 0, -d)));
                gradient.z += 4.0 * (SampleVolume(pos + float3(0, 0, d)) - SampleVolume(pos + float3(0, 0, -d)));
                gradient.z += w1 * (SampleVolume(pos + float3(d, 0, d)) - SampleVolume(pos + float3(d, 0, -d)));
                gradient.z += w0 * (SampleVolume(pos + float3(-d, d, d)) - SampleVolume(pos + float3(-d, d, -d)));
                gradient.z += w1 * (SampleVolume(pos + float3(0, d, d)) - SampleVolume(pos + float3(0, d, -d)));
                gradient.z += w0 * (SampleVolume(pos + float3(d, d, d)) - SampleVolume(pos + float3(d, d, -d)));

                return normalize(gradient + 0.0001);
            }

            // Simple central difference gradient (faster)
            float3 CalculateGradientSimple(float3 pos)
            {
                float d = _StepSize;
                float3 gradient;
                gradient.x = SampleVolume(pos + float3(d, 0, 0)) - SampleVolume(pos - float3(d, 0, 0));
                gradient.y = SampleVolume(pos + float3(0, d, 0)) - SampleVolume(pos - float3(0, d, 0));
                gradient.z = SampleVolume(pos + float3(0, 0, d)) - SampleVolume(pos - float3(0, 0, d));
                return normalize(gradient + 0.0001);
            }

            // Calculate ambient occlusion
            float CalculateAO(float3 pos, float3 normal)
            {
                if (_UseAO < 0.5) return 1.0;

                float ao = 0.0;
                float sampleRadius = _AORadius;

                for (int i = 0; i < _AOSamples; i++)
                {
                    float angle = (float)i / (float)_AOSamples * 6.28318;
                    float3 sampleDir = normalize(normal + float3(cos(angle) * 0.5, sin(angle) * 0.5, 0));
                    float3 samplePos = pos + sampleDir * sampleRadius;

                    if (samplePos.x >= 0 && samplePos.x <= 1 &&
                        samplePos.y >= 0 && samplePos.y <= 1 &&
                        samplePos.z >= 0 && samplePos.z <= 1)
                    {
                        float sampleDensity = SampleVolume(samplePos);
                        sampleDensity = ApplyWindowLevel(sampleDensity);
                        ao += step(_MinThreshold, sampleDensity);
                    }
                }

                ao = 1.0 - (ao / (float)_AOSamples) * _AOIntensity;
                return saturate(ao);
            }

            // Subsurface scattering approximation
            float3 CalculateSSS(float3 lightDir, float3 viewDir, float3 normal, float3 baseColor)
            {
                if (_UseSSS < 0.5) return float3(0, 0, 0);

                float3 H = normalize(lightDir + normal * _SSSDistortion);
                float VdotH = pow(saturate(dot(viewDir, -H)), _SSSPower);
                float3 sss = _SSSColor.rgb * VdotH * _SSSIntensity;

                return sss * baseColor;
            }

            // Rim lighting
            float3 CalculateRimLight(float3 viewDir, float3 normal)
            {
                if (_UseRimLight < 0.5) return float3(0, 0, 0);

                float rim = 1.0 - saturate(dot(viewDir, normal));
                rim = pow(rim, _RimPower);

                return _RimColor.rgb * rim * _RimIntensity;
            }

            // Apply enhanced lighting
            float3 ApplyEnhancedLighting(float3 color, float3 normal, float3 viewDir, float3 pos, float ao)
            {
                float3 lightDir = normalize(_LightDir);

                // Ambient with AO
                float3 ambient = _AmbientColor.rgb * _AmbientLight * ao;

                // Diffuse (half-lambert for softer look)
                float NdotL = dot(normal, lightDir);
                float diffuseTerm = NdotL * 0.5 + 0.5; // Half-Lambert
                diffuseTerm = pow(diffuseTerm, 1.5); // Soften falloff
                float3 diffuse = _LightColor.rgb * _DiffuseLight * diffuseTerm;

                // Specular (Blinn-Phong)
                float3 halfDir = normalize(lightDir + viewDir);
                float NdotH = max(dot(normal, halfDir), 0.0);
                float specTerm = pow(NdotH, _Shininess);
                float3 specular = _LightColor.rgb * _SpecularLight * specTerm;

                // Subsurface scattering
                float3 sss = CalculateSSS(lightDir, viewDir, normal, color);

                // Rim lighting
                float3 rim = CalculateRimLight(viewDir, normal);

                // Combine all lighting
                float3 finalColor = color * (ambient + diffuse) + specular + sss + rim;

                return finalColor;
            }

            // Color grading
            float3 ApplyColorGrading(float3 color)
            {
                // Saturation
                float luminance = dot(color, float3(0.299, 0.587, 0.114));
                color = lerp(float3(luminance, luminance, luminance), color, _Saturation);

                // Contrast
                color = (color - 0.5) * _Contrast + 0.5;

                // Brightness
                color *= _Brightness;

                // Gamma correction
                color = pow(max(color, 0.0001), 1.0 / _Gamma);

                return saturate(color);
            }

            // Check clipping plane
            bool IsClipped(float3 worldPos)
            {
                if (_UseClipPlane < 0.5) return false;
                float3 toPoint = worldPos - _ClipPlanePosition;
                return dot(toPoint, normalize(_ClipPlaneNormal)) < 0;
            }

            // Ray-box intersection
            bool IntersectBox(float3 rayOrigin, float3 rayDir, out float tNear, out float tFar)
            {
                float3 invRay = 1.0 / (rayDir + 0.0001);
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
                // Calculate ray
                float3 rayOrigin = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1)).xyz + 0.5;
                float3 rayDir = normalize(i.localPos - rayOrigin);

                // Find intersection
                float tNear, tFar;
                if (!IntersectBox(rayOrigin, rayDir, tNear, tFar))
                    discard;

                tNear = max(tNear, 0.0);

                // Jittering for anti-aliasing
                float jitter = 0;
                if (_UseJittering > 0.5)
                {
                    jitter = noise3D(i.localPos * 100 + _Time.y * 10) * _StepSize * 0.5;
                }

                // Initialize accumulation
                float4 accumulatedColor = float4(0, 0, 0, 0);
                float3 viewDir = normalize(i.viewDir);

                // Ray marching
                float t = tNear + jitter;
                float prevDensity = 0;

                [loop]
                for (int j = 0; j < _Iterations && t < tFar && accumulatedColor.a < 0.99; j++)
                {
                    float3 samplePos = rayOrigin + rayDir * t;

                    // Bounds check
                    if (any(samplePos < 0) || any(samplePos > 1))
                    {
                        t += _StepSize;
                        continue;
                    }

                    // Clipping plane check
                    float3 worldSamplePos = mul(unity_ObjectToWorld, float4(samplePos - 0.5, 1)).xyz;
                    if (IsClipped(worldSamplePos))
                    {
                        t += _StepSize;
                        continue;
                    }

                    // Sample volume
                    float density = SampleVolume(samplePos);
                    density = ApplyWindowLevel(density);

                    // Threshold check
                    if (density < _MinThreshold || density > _MaxThreshold)
                    {
                        prevDensity = density;
                        t += _StepSize;
                        continue;
                    }

                    // Get color from transfer function
                    float4 sampleColor = SampleTransferFunction(density);

                    // Adaptive opacity based on step size
                    float adaptiveAlpha = 1.0 - pow(1.0 - sampleColor.a, _StepSize * 100 * _Density);
                    sampleColor.a = adaptiveAlpha * _Alpha;

                    // Apply lighting for visible samples
                    if (sampleColor.a > 0.005)
                    {
                        float3 gradient = CalculateGradientSimple(samplePos);
                        float gradientMag = length(gradient);

                        if (gradientMag > 0.05)
                        {
                            float3 normal = gradient;

                            // Calculate AO
                            float ao = CalculateAO(samplePos, normal);

                            // Apply enhanced lighting
                            sampleColor.rgb = ApplyEnhancedLighting(sampleColor.rgb, normal, viewDir, samplePos, ao);
                        }
                    }

                    // Front-to-back compositing
                    sampleColor.rgb *= sampleColor.a;
                    accumulatedColor += (1.0 - accumulatedColor.a) * sampleColor;

                    prevDensity = density;
                    t += _StepSize;
                }

                // Discard transparent fragments
                if (accumulatedColor.a < 0.005)
                    discard;

                // Apply color grading
                accumulatedColor.rgb = ApplyColorGrading(accumulatedColor.rgb);

                // Depth fade
                float depth = length(_WorldSpaceCameraPos - i.worldPos);
                float depthFade = 1.0 - saturate((depth - _DepthFadeDistance) * _DepthFade);
                accumulatedColor.a *= depthFade;

                return accumulatedColor;
            }
            ENDCG
        }
    }

    FallBack "Transparent/Diffuse"
}
