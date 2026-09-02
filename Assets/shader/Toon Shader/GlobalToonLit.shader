Shader "PJPR/Toon/Global Toon Lit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _ShadeColor("Shade Color", Color) = (0.45, 0.5, 0.65, 1)
        _ThirdColor("Third Shade Color", Color) = (0.18, 0.22, 0.35, 1)

        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Strength", Range(0, 2)) = 1

        _OcclusionMap("Ambient Occlusion", 2D) = "white" {}
        _OcclusionStrength("Ambient Occlusion Strength", Range(0, 1)) = 1

        _ShadowBoundary("Shadow Edge Boundary", Range(0, 1)) = 0.48
        _ShadowSoftness("Shadow Edge Softness", Range(0.001, 0.5)) = 0.035
        _ThirdColorStrength("Third Shade Strength", Range(0, 1)) = 0.65
        _ThirdColorSize("Third Shade Size", Range(0.001, 1)) = 0.45
        _AmbientStrength("Ambient Strength", Range(0, 2)) = 0.35
        _AdditionalLightStrength("Additional Light Strength", Range(0, 2)) = 1
        _AOIntensity("Global AO Intensity", Range(0, 2)) = 1

        [ToggleUI] _UseGlobalToonSettings("Use Global Toon Settings", Float) = 1
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clipping", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        _Cull("Cull", Float) = 2

        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ToonPassVertex
            #pragma fragment ToonPassFragment

            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_OcclusionMap);
            SAMPLER(sampler_OcclusionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShadeColor;
                half4 _ThirdColor;
                half _BumpScale;
                half _OcclusionStrength;
                half _ShadowBoundary;
                half _ShadowSoftness;
                half _ThirdColorStrength;
                half _ThirdColorSize;
                half _AmbientStrength;
                half _AdditionalLightStrength;
                half _AOIntensity;
                half _UseGlobalToonSettings;
                half _Cutoff;
                half _Cull;
            CBUFFER_END

            half4 _PJPRToonBaseColor;
            half4 _PJPRToonShadeColor;
            half4 _PJPRToonThirdColor;
            half _PJPRToonSettingsActive;
            half _PJPRToonNormalStrength;
            half _PJPRToonOcclusionStrength;
            half _PJPRToonShadowBoundary;
            half _PJPRToonShadowSoftness;
            half _PJPRToonThirdColorStrength;
            half _PJPRToonThirdColorSize;
            half _PJPRToonAmbientStrength;
            half _PJPRToonAdditionalLightStrength;
            half _PJPRToonAOIntensity;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
                float2 screenUV : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct ToonSettings
            {
                half3 baseColor;
                half3 shadeColor;
                half3 thirdColor;
                half normalStrength;
                half occlusionStrength;
                half shadowBoundary;
                half shadowSoftness;
                half thirdColorStrength;
                half thirdColorSize;
                half ambientStrength;
                half additionalLightStrength;
                half aoIntensity;
            };

            Varyings ToonPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = half4(normalInputs.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.shadowCoord = TransformWorldToShadowCoord(output.positionWS);
                output.screenUV = GetNormalizedScreenSpaceUV(output.positionCS);

                return output;
            }

            ToonSettings ResolveToonSettings()
            {
                half useGlobal = step(0.5h, _UseGlobalToonSettings * _PJPRToonSettingsActive);

                ToonSettings settings;
                settings.baseColor = lerp(_BaseColor.rgb, _PJPRToonBaseColor.rgb, useGlobal);
                settings.shadeColor = lerp(_ShadeColor.rgb, _PJPRToonShadeColor.rgb, useGlobal);
                settings.thirdColor = lerp(_ThirdColor.rgb, _PJPRToonThirdColor.rgb, useGlobal);
                settings.normalStrength = max(0.0h, lerp(_BumpScale, _PJPRToonNormalStrength, useGlobal));
                settings.occlusionStrength = saturate(lerp(_OcclusionStrength, _PJPRToonOcclusionStrength, useGlobal));
                settings.shadowBoundary = saturate(lerp(_ShadowBoundary, _PJPRToonShadowBoundary, useGlobal));
                settings.shadowSoftness = max(0.001h, lerp(_ShadowSoftness, _PJPRToonShadowSoftness, useGlobal));
                settings.thirdColorStrength = saturate(lerp(_ThirdColorStrength, _PJPRToonThirdColorStrength, useGlobal));
                settings.thirdColorSize = max(0.001h, lerp(_ThirdColorSize, _PJPRToonThirdColorSize, useGlobal));
                settings.ambientStrength = max(0.0h, lerp(_AmbientStrength, _PJPRToonAmbientStrength, useGlobal));
                settings.additionalLightStrength = max(0.0h, lerp(_AdditionalLightStrength, _PJPRToonAdditionalLightStrength, useGlobal));
                settings.aoIntensity = max(0.0h, lerp(_AOIntensity, _PJPRToonAOIntensity, useGlobal));
                return settings;
            }

            half3 GetNormalWS(Varyings input, ToonSettings settings)
            {
                half3 tangentWS = normalize(input.tangentWS.xyz);
                half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                half tangentSign = input.tangentWS.w;
                half3 bitangentWS = cross(normalWS, tangentWS) * tangentSign;
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), settings.normalStrength);

                return NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, half3x3(tangentWS, bitangentWS, normalWS)));
            }

            half GetToonAmount(half lightAmount, ToonSettings settings)
            {
                half low = settings.shadowBoundary - settings.shadowSoftness;
                half high = settings.shadowBoundary + settings.shadowSoftness;
                return smoothstep(low, high, lightAmount);
            }

            half3 GetToonColor(half lightAmount, ToonSettings settings)
            {
                half toonAmount = GetToonAmount(lightAmount, settings);
                half shadeDepth = saturate((settings.shadowBoundary - lightAmount) / max(settings.shadowBoundary, 0.001h));
                half thirdMask = smoothstep(1.0h - settings.thirdColorSize, 1.0h, shadeDepth) * settings.thirdColorStrength;
                half3 shade = lerp(settings.shadeColor, settings.thirdColor, thirdMask);

                return lerp(shade, settings.baseColor, toonAmount);
            }

            half GetLightAmount(Light light, half3 normalWS, half strength)
            {
                half ndotl = saturate(dot(normalWS, light.direction));
                return saturate(ndotl * light.distanceAttenuation * light.shadowAttenuation * strength);
            }

            half4 ToonPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half alpha = baseSample.a * _BaseColor.a;

                #if defined(_ALPHATEST_ON)
                    clip(alpha - _Cutoff);
                #endif

                ToonSettings settings = ResolveToonSettings();
                half3 normalWS = GetNormalWS(input, settings);
                half3 viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirectionWS;
                inputData.shadowCoord = input.shadowCoord;
                inputData.normalizedScreenSpaceUV = input.screenUV;
                inputData.shadowMask = half4(1, 1, 1, 1);
                inputData.bakedGI = SampleSHPixel(half3(0, 0, 0), normalWS);

                half occlusionMap = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).g;
                half materialOcclusion = lerp(1.0h, occlusionMap, settings.occlusionStrength);
                AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData.normalizedScreenSpaceUV, materialOcclusion);
                half ao = lerp(1.0h, aoFactor.indirectAmbientOcclusion * aoFactor.directAmbientOcclusion, saturate(settings.aoIntensity));
                half4 shadowMask = CalculateShadowMask(inputData);

                Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
                half mainAmount = GetLightAmount(mainLight, normalWS, 1.0h);
                half totalLightAmount = mainAmount;
                half3 weightedLightColor = mainLight.color * max(mainAmount, 0.001h);

                #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
                    uint pixelLightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
                        #if defined(_LIGHT_LAYERS)
                            if (IsMatchingLightLayer(light.layerMask, GetMeshRenderingLayer()))
                        #endif
                        {
                            half additionalAmount = GetLightAmount(light, normalWS, settings.additionalLightStrength);
                            totalLightAmount = saturate(totalLightAmount + additionalAmount);
                            weightedLightColor += light.color * additionalAmount;
                        }
                    LIGHT_LOOP_END
                #endif

                half3 lightColor = weightedLightColor / max(totalLightAmount, 0.001h);
                half3 toonColor = GetToonColor(totalLightAmount, settings);
                half3 ambient = inputData.bakedGI * settings.ambientStrength * aoFactor.indirectAmbientOcclusion;
                half3 direct = lightColor * lerp(0.35h, 1.0h, GetToonAmount(totalLightAmount, settings));
                half3 color = baseSample.rgb * toonColor * (ambient + direct) * ao;

                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
        UsePass "Universal Render Pipeline/Lit/Meta"
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
