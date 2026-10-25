using System;
using System.IO;
using Avalonia;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Avalonia.Editor
{
    public static class ConfigurarProgressaoAvalonia
    {
        private const string CaminhoCena = "Assets/Scenes/AvalonyaWorld.unity";
        private const string PastaOrigem = "Assets/Art/Sprites/XP";
        private const string PastaGerada = "Assets/Art/Generated/XP";
        private const string CaminhoPrefab = "Assets/Prefabs/JoiaXpAvalonia.prefab";

        [MenuItem("Avalonia/Migracao/Configurar base de XP e abates")]
        public static void Configurar()
        {
            Directory.CreateDirectory(Path.GetFullPath(PastaOrigem));
            Directory.CreateDirectory(Path.GetFullPath(PastaGerada));
            string caminhoAtlas = Path.GetFullPath($"{PastaOrigem}/Xp_drop.png");
            if (!File.Exists(caminhoAtlas))
            {
                throw new FileNotFoundException("Copie o atlas Xp_drop.png para Assets/Art/Sprites/XP antes de configurar o XP.", caminhoAtlas);
            }

            GerarRecorteDaJoia();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigurarImportacao();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            JoiaXpAvalonia prefabJoia = CriarPrefabJoia();
            ConfigurarCena(prefabJoia);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Base de progresso configurada: abates, joias de XP e dados da partida.");
        }

        private static void GerarRecorteDaJoia()
        {
            string caminhoFonte = Path.GetFullPath($"{PastaOrigem}/Xp_drop.png");
            Texture2D atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Texture2D recorte = null;
            try
            {
                if (!atlas.LoadImage(File.ReadAllBytes(caminhoFonte), false))
                {
                    throw new InvalidDataException("O atlas de XP não pôde ser carregado.");
                }

                const int x = 214;
                const int yTopo = 238;
                const int largura = 33;
                const int altura = 53;
                if (x + largura > atlas.width || yTopo + altura > atlas.height)
                {
                    throw new InvalidDataException("O recorte da joia excede os limites do atlas de XP.");
                }

                recorte = new Texture2D(largura, altura, TextureFormat.RGBA32, false);
                Color[] pixels = atlas.GetPixels(x, atlas.height - yTopo - altura, largura, altura);
                recorte.SetPixels(pixels);
                recorte.Apply(false, false);
                File.WriteAllBytes(Path.GetFullPath($"{PastaGerada}/JoiaXp.png"), recorte.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(atlas);
                if (recorte != null)
                {
                    UnityEngine.Object.DestroyImmediate(recorte);
                }
            }
        }

        private static void ConfigurarImportacao()
        {
            TextureImporter importador = AssetImporter.GetAtPath($"{PastaGerada}/JoiaXp.png") as TextureImporter;
            if (importador == null)
            {
                throw new FileNotFoundException("O importador da joia de XP não foi encontrado.");
            }

            importador.textureType = TextureImporterType.Sprite;
            importador.spriteImportMode = SpriteImportMode.Single;
            importador.spritePixelsPerUnit = 100f;
            importador.maxTextureSize = 256;
            importador.textureCompression = TextureImporterCompression.Uncompressed;
            importador.alphaIsTransparency = true;
            importador.filterMode = FilterMode.Bilinear;
            importador.mipmapEnabled = false;
            importador.SaveAndReimport();
        }

        private static JoiaXpAvalonia CriarPrefabJoia()
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PastaGerada}/JoiaXp.png");
            if (sprite == null)
            {
                throw new FileNotFoundException("O recorte da joia de XP não foi importado como Sprite.");
            }

            GameObject raiz = new GameObject("JoiaXpAvalonia");
            try
            {
                SpriteRenderer renderizador = raiz.AddComponent<SpriteRenderer>();
                renderizador.sprite = sprite;
                renderizador.sortingOrder = 20;
                renderizador.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderizador.receiveShadows = false;

                Light brilho = raiz.AddComponent<Light>();
                brilho.type = LightType.Point;
                brilho.color = new Color(0.93f, 0.65f, 1f, 1f);
                brilho.intensity = 2.4f;
                brilho.range = 2f;
                brilho.shadows = LightShadows.None;
                brilho.enabled = false;

                JoiaXpAvalonia joia = raiz.AddComponent<JoiaXpAvalonia>();
                SerializedObject serializado = new SerializedObject(joia);
                serializado.FindProperty("visual").objectReferenceValue = renderizador;
                serializado.FindProperty("brilho").objectReferenceValue = brilho;
                serializado.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab, out bool sucesso);
                if (!sucesso)
                {
                    throw new IOException("Unity não conseguiu salvar o prefab de joia de XP.");
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefab);
                return prefab != null ? prefab.GetComponent<JoiaXpAvalonia>() : null;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(raiz);
            }
        }

        private static void ConfigurarCena(JoiaXpAvalonia prefabJoia)
        {
            Scene cena = EditorSceneManager.OpenScene(CaminhoCena, OpenSceneMode.Single);
            GameObject objetoEstado = GameObject.Find("EstadoJogoAvalonia");
            if (objetoEstado == null)
            {
                objetoEstado = new GameObject("EstadoJogoAvalonia");
                SceneManager.MoveGameObjectToScene(objetoEstado, cena);
            }

            EstadoJogoAvalonia estado = objetoEstado.GetComponent<EstadoJogoAvalonia>();
            if (estado == null)
            {
                estado = objetoEstado.AddComponent<EstadoJogoAvalonia>();
            }

            SerializedObject serializado = new SerializedObject(estado);
            serializado.FindProperty("prefabJoiaXp").objectReferenceValue = prefabJoia;
            serializado.ApplyModifiedPropertiesWithoutUndo();
            estado.Configurar(prefabJoia);
            EditorSceneManager.MarkSceneDirty(cena);
            if (!EditorSceneManager.SaveScene(cena))
            {
                throw new IOException("Unity não conseguiu salvar o estado e as referências de XP na cena.");
            }
        }
    }
}
