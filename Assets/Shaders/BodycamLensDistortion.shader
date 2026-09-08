Shader ""Custom/BodycamLensDistortion""
{
    Properties
    {
        _MainTex ("Source Texture", 2D) = ""white"" {}
        _Distortion (""Barrel Distortion (Fisheye)"", Range(-1.0, 1.0)) = 0.25
        _CubicDistortion (""Cubic Distortion"", Range(-1.0, 1.0)) = 0.15
        _ChromaticAberration (""Chromatic Aberration"", Range(0.0, 0.05)) = 0.012
        _VignettePower (""Vignette Power"", Range(0.0, 5.0)) = 1.8
        _VignetteIntensity (""Vignette Intensity"", Range(0.0, 1.0)) = 0.45
        _FilmGrainIntensity (""Film Grain Intensity"", Range(0.0, 0.2)) = 0.04
    }

    SubShader
    {
        Tags { ""RenderType"" = ""Opaque"" ""RenderPipeline"" = ""UniversalPipeline"" }
        LOD 100
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            Name ""BodycamPostPass""

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl""

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Texture2D _MainTex;
            SamplerState sampler_MainTex;

            float _Distortion;
            float _CubicDistortion;
            float _ChromaticAberration;
            float _VignettePower;
            float _VignetteIntensity;
            float _FilmGrainIntensity;

            // Генератор полноэкранного треугольника
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            // Псевдослучайный шум для симуляции зерна матрицы сенсора
            float GenerateNoise(float2 uv, float time)
            {
                return frac(sin(dot(uv + time, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Формула дисторсии объектива типа ""рыбий глаз"" (Brown-Conrady Barrel Distortion)
            float2 ApplyLensDistortion(float2 uv)
            {
                float2 centered = uv - 0.5;
                float r2 = dot(centered, centered);
                float f = 1.0 + r2 * (_Distortion + _CubicDistortion * sqrt(r2));
                return 0.5 + centered * f;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 distortedUV = ApplyLensDistortion(input.uv);

                // Черные полосы за пределами искривленной линзы
                if (distortedUV.x < 0.0 || distortedUV.x > 1.0 || distortedUV.y < 0.0 || distortedUV.y > 1.0)
                {
                    return half4(0.0, 0.0, 0.0, 1.0);
                }

                // 1. Хроматические аберрации (радиальное расслоение RGB каналов)
                float2 dir = distortedUV - 0.5;
                float dist = length(dir);
                float2 caOffset = dir * (dist * _ChromaticAberration);

                half r = _MainTex.Sample(sampler_MainTex, distortedUV + caOffset).r;
                half g = _MainTex.Sample(sampler_MainTex, distortedUV).g;
                half b = _MainTex.Sample(sampler_MainTex, distortedUV - caOffset).b;
                half3 color = half3(r, g, b);

                // 2. Виньетирование по краям кадра
                float vignette = dist * 1.414; // Нормализация к углам
                vignette = saturate(pow(vignette, _VignettePower) * _VignetteIntensity);
                color = lerp(color, color * (1.0 - vignette), vignette);

                // 3. Пленочное зерно бодикамеры
                float grain = (GenerateNoise(input.uv, _Time.y * 2.0) - 0.5) * _FilmGrainIntensity;
                color += grain;

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack ""Hidden/Universal Render Pipeline/Blit""
}
