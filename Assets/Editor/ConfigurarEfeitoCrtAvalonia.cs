using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Reflection;

namespace Avalonia.Editor
{
    public static class ConfigurarEfeitoCrtAvalonia
    {
        private const string CaminhoShader = "Assets/Art/Menu/ScreenEffect/CRTMenuAvalonia.shader";
        private const string CaminhoMaterial = "Assets/Art/Materials/CRTMenuAvalonia.mat";
        private const string NomeEfeito = "CRT Menu Avalonia";

        [MenuItem("Avalonia/Migracao/Configurar filtro CRT do menu")]
        public static void Configurar()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(CaminhoShader);
            if (shader == null)
            {
                throw new System.InvalidOperationException("O shader CRT do menu não foi importado.");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(CaminhoMaterial);
            if (material == null)
            {
                material = new Material(shader) { name = "CRTMenuAvalonia" };
                AssetDatabase.CreateAsset(material, CaminhoMaterial);
            }
            else
            {
                material.shader = shader;
            }

            material.SetFloat("_CRTCurve", 0.023f);
            material.SetFloat("_CRTScanLineColor", 0.01f);
            material.SetFloat("_ApertureGrilleRate", 0.222f);
            material.SetFloat("_ApertureGrilleCutRate", 0.4f);
            material.SetFloat("_RFBlur", 0.292f);
            material.SetFloat("_WhiteNoiseRate", 0.132f);
            EditorUtility.SetDirty(material);

            ConfigurarRenderizador(AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset"), material);
            ConfigurarRenderizador(AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/Mobile_Renderer.asset"), material);
            AssetDatabase.SaveAssets();
            Debug.Log("Filtro CRT configurado nos renderizadores PC e Mobile; fica ativo apenas na cena do menu.");
        }

        private static void ConfigurarRenderizador(UniversalRendererData dados, Material material)
        {
            if (dados == null)
            {
                throw new System.InvalidOperationException("Um renderizador URP não foi encontrado.");
            }

            FullScreenPassRendererFeature recurso = null;
            foreach (ScriptableRendererFeature existente in dados.rendererFeatures)
            {
                if (existente is FullScreenPassRendererFeature filtro && existente.name == NomeEfeito)
                {
                    recurso = filtro;
                    break;
                }
            }

            if (recurso == null)
            {
                recurso = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                recurso.name = NomeEfeito;
                recurso.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(recurso, dados);
                dados.rendererFeatures.Add(recurso);
            }

            recurso.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
            recurso.fetchColorBuffer = true;
            recurso.requirements = ScriptableRenderPassInput.None;
            recurso.passMaterial = material;
            recurso.passIndex = 0;
            recurso.bindDepthStencilAttachment = false;
            recurso.SetActive(false);
            EditorUtility.SetDirty(recurso);
            dados.SetDirty();
            EditorUtility.SetDirty(dados);

            MethodInfo validarRecursos = typeof(ScriptableRendererData).GetMethod(
                "ValidateRendererFeatures",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (validarRecursos == null || !(bool)validarRecursos.Invoke(dados, null))
            {
                throw new System.InvalidOperationException($"O renderizador {dados.name} não validou seus recursos.");
            }

            FieldInfo campoMapa = typeof(ScriptableRendererData).GetField(
                "m_RendererFeatureMap",
                BindingFlags.Instance | BindingFlags.NonPublic);
            System.Collections.ICollection mapa = campoMapa?.GetValue(dados) as System.Collections.ICollection;
            if (campoMapa == null || mapa == null || mapa.Count != dados.rendererFeatures.Count)
            {
                throw new System.InvalidOperationException($"O mapa de recursos do renderizador {dados.name} continua incompleto.");
            }

            foreach (ScriptableRendererFeature feature in dados.rendererFeatures)
            {
                if (feature == null)
                {
                    throw new System.InvalidOperationException($"O renderizador {dados.name} contém um recurso ausente.");
                }
            }
        }
    }
}
