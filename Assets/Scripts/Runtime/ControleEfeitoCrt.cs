using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Avalonya
{
    /// <summary>Ativa o efeito CRT apenas enquanto a cena do menu está aberta.</summary>
    public static class ControleEfeitoCrt
    {
        private const string NomeEfeito = "CRT Menu Avalonya";

        public static void DefinirAtivo(bool ativo)
        {
            UniversalRenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null)
            {
                return;
            }

            var renderizadores = pipeline.rendererDataList;
            for (int indice = 0; indice < renderizadores.Length; indice++)
            {
                ScriptableRendererData dados = renderizadores[indice];
                if (dados == null)
                {
                    continue;
                }

                foreach (ScriptableRendererFeature recurso in dados.rendererFeatures)
                {
                    if (recurso is FullScreenPassRendererFeature && recurso.name == NomeEfeito)
                    {
                        recurso.SetActive(ativo);
                    }
                }
            }
        }
    }
}
