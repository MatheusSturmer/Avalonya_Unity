Shader "Avalonia/CRT Menu"
{
    Properties
    {
        _CRTCurve ("Curvatura", Range(0, 1)) = 0.023
        _CRTScanLineColor ("Intensidade das linhas", Range(0, 1)) = 0.01
        _ApertureGrilleRate ("Abertura do grille", Range(0, 1)) = 0.222
        _ApertureGrilleCutRate ("Corte do grille", Range(0, 1)) = 0.4
        _RFBlur ("Desfoque horizontal", Range(0, 1)) = 0.292
        _WhiteNoiseRate ("Ruido", Range(0, 1)) = 0.132
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "CRT Menu"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _CRTCurve;
            float _CRTScanLineColor;
            float _ApertureGrilleRate;
            float _ApertureGrilleCutRate;
            float _RFBlur;
            float _WhiteNoiseRate;

            float Aleatorio(float2 pos)
            {
                return frac(sin(dot(pos, float2(12.9898, 78.233))) * 43758.5453);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float2 curvaDeslocamento = (1.0 - sin(uv.yx * 3.14159265)) * _CRTCurve;
                float2 curvaEscala = 1.0 + curvaDeslocamento * 2.0;
                float2 uvCurva = uv * curvaEscala - curvaDeslocamento;
                float dentroDaTela = step(0.0, uvCurva.x) * step(uvCurva.x, 1.0)
                    * step(0.0, uvCurva.y) * step(uvCurva.y, 1.0);

                float2 deslocamentoRf = float2(_BlitTexture_TexelSize.x * 3.1, 0.0);
                float3 original = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvCurva).rgb;
                float3 esquerda = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvCurva - deslocamentoRf).rgb;
                float3 direita = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvCurva + deslocamentoRf).rgb;
                float3 cor = (original * (1.0 - _RFBlur * 0.5) + (esquerda + direita) * (_RFBlur * 0.25)) * dentroDaTela;

                float2 pixelGrade = float2(floor((uvCurva.x / _BlitTexture_TexelSize.x) / 3.0) * 3.0, uvCurva.y);
                float ruido = Aleatorio(pixelGrade + float2(sin(_Time.y * 0.543254), cos(_Time.y * 0.254323563)));
                cor = lerp(cor, ruido.xxx, _WhiteNoiseRate * dentroDaTela);

                float pontoGrade = fmod(uvCurva.x / _BlitTexture_TexelSize.x, 3.0);
                float taxaVermelho = saturate(1.0 - pontoGrade) + saturate(pontoGrade - 2.0);
                float taxaVerde = saturate(1.0 - abs(1.0 - pontoGrade));
                float taxaAzul = 1.0 - taxaVermelho - taxaVerde;
                float3 taxas = normalize(float3(
                    clamp(taxaVermelho, _ApertureGrilleRate, 1.0),
                    clamp(taxaVerde, _ApertureGrilleRate, 1.0),
                    clamp(taxaAzul, _ApertureGrilleRate, 1.0)
                ));
                cor = clamp(cor * taxas, original * _ApertureGrilleCutRate, 1.0);

                float linhaEscura = 1.0 - fmod(floor(uvCurva.y / _BlitTexture_TexelSize.y), 2.0);
                cor = lerp(cor, 0.0.xxx, linhaEscura * _CRTScanLineColor);
                return float4(cor, 1.0);
            }
            ENDHLSL
        }
    }
}
