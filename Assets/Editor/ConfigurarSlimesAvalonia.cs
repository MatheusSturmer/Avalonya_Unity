using System.IO;
using Avalonia;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Avalonia.Editor
{
    public static class ConfigurarSlimesAvalonia
    {
        private const string CaminhoCena = "Assets/Scenes/AvalonyaWorld.unity";
        private const string CaminhoPastaPrefabs = "Assets/Prefabs";
        private const string CaminhoPrefab = "Assets/Prefabs/SlimeAvalonia.prefab";
        private const string CaminhoSlimes = "Assets/Art/Sprites/Slime";
        private const float PixelsPorUnidade = 181.81818f;

        [MenuItem("Avalonia/Migracao/Configurar slime e surgimento")]
        public static void ConfigurarSlimeESurgimento()
        {
            ConfigurarImportacaoSprites();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            GarantirPasta(CaminhoPastaPrefabs);

            InimigoSlimeAvalonia prefab = CriarOuAtualizarPrefab();
            ConfigurarCena(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Etapa 3A configurada: slime animado persegue o jogador e surge em ondas; ataques ficam para a etapa de vida.");
        }

        private static InimigoSlimeAvalonia CriarOuAtualizarPrefab()
        {
            GameObject raiz = new GameObject("SlimeAvalonia");
            try
            {
                CharacterController colisor = raiz.AddComponent<CharacterController>();
                colisor.height = 1.15f;
                colisor.radius = 0.4f;
                colisor.center = new Vector3(0f, 0.575f, 0f);
                colisor.skinWidth = 0.05f;
                colisor.stepOffset = 0.15f;
                colisor.minMoveDistance = 0f;

                GameObject objetoVisual = new GameObject("VisualSlime");
                objetoVisual.transform.SetParent(raiz.transform, false);
                objetoVisual.transform.localPosition = new Vector3(0f, 0.63f, 0f);

                SpriteRenderer renderizador = objetoVisual.AddComponent<SpriteRenderer>();
                renderizador.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderizador.receiveShadows = false;
                renderizador.sortingOrder = 11;

                InimigoSlimeAvalonia slime = raiz.AddComponent<InimigoSlimeAvalonia>();
                SerializedObject serializado = new SerializedObject(slime);
                serializado.FindProperty("visual").objectReferenceValue = renderizador;
                serializado.FindProperty("quadrosAndar").arraySize = 2;
                serializado.FindProperty("quadrosAndar").GetArrayElementAtIndex(0).objectReferenceValue = CarregarSprite("walk_1.png");
                serializado.FindProperty("quadrosAndar").GetArrayElementAtIndex(1).objectReferenceValue = CarregarSprite("walk_2.png");
                serializado.FindProperty("velocidade").floatValue = 2f;
                serializado.FindProperty("distanciaParar").floatValue = 1.45f;
                serializado.FindProperty("raioSeparacao").floatValue = 1.2f;
                serializado.FindProperty("forcaSeparacao").floatValue = 1.5f;
                serializado.FindProperty("quadrosPorSegundo").floatValue = 1.8f;
                serializado.ApplyModifiedPropertiesWithoutUndo();

                GameObject prefabExistente = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefab);
                if (prefabExistente != null)
                {
                    PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab, out bool sucesso);
                    if (!sucesso)
                    {
                        throw new IOException("Unity nao conseguiu atualizar o prefab SlimeAvalonia.");
                    }
                }
                else
                {
                    PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab, out bool sucesso);
                    if (!sucesso)
                    {
                        throw new IOException("Unity nao conseguiu criar o prefab SlimeAvalonia.");
                    }
                }

                return AssetDatabase.LoadAssetAtPath<InimigoSlimeAvalonia>(CaminhoPrefab);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static void ConfigurarCena(InimigoSlimeAvalonia prefab)
        {
            if (prefab == null)
            {
                throw new FileNotFoundException("O prefab do slime nao foi criado.", CaminhoPrefab);
            }

            Scene cena = EditorSceneManager.OpenScene(CaminhoCena, OpenSceneMode.Single);
            GameObject objetoJogador = GameObject.Find("JogadorAvalonia");
            GameObject objetoSpawner = GameObject.Find("SlimeSpawn");
            if (objetoJogador == null || objetoSpawner == null)
            {
                throw new System.InvalidOperationException("JogadorAvalonia ou SlimeSpawn nao foi encontrado na cena.");
            }

            SpawnerSlimeAvalonia spawner = objetoSpawner.GetComponent<SpawnerSlimeAvalonia>();
            if (spawner == null)
            {
                spawner = objetoSpawner.AddComponent<SpawnerSlimeAvalonia>();
            }

            spawner.Configurar(objetoJogador.transform, prefab);
            SerializedObject spawnerSerializado = new SerializedObject(spawner);
            spawnerSerializado.FindProperty("raioMinimo").floatValue = 15f;
            spawnerSerializado.FindProperty("raioMaximo").floatValue = 30f;
            spawnerSerializado.FindProperty("atrasoInicial").floatValue = 1f;
            spawnerSerializado.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena, CaminhoCena);
        }

        private static void ConfigurarImportacaoSprites()
        {
            string[] arquivos = { "walk_1.png", "walk_2.png", "attack_1.png", "attack_2.png" };
            foreach (string arquivo in arquivos)
            {
                string caminho = $"{CaminhoSlimes}/{arquivo}";
                TextureImporter importador = AssetImporter.GetAtPath(caminho) as TextureImporter;
                if (importador == null)
                {
                    throw new FileNotFoundException("O importador de sprite do slime nao foi encontrado.", caminho);
                }

                importador.textureType = TextureImporterType.Sprite;
                importador.spriteImportMode = SpriteImportMode.Single;
                importador.spritePixelsPerUnit = PixelsPorUnidade;
                importador.maxTextureSize = 2048;
                importador.textureCompression = TextureImporterCompression.Uncompressed;
                importador.npotScale = TextureImporterNPOTScale.None;
                importador.alphaIsTransparency = true;
                importador.filterMode = FilterMode.Point;
                importador.mipmapEnabled = false;
                importador.SaveAndReimport();
            }
        }

        private static Sprite CarregarSprite(string arquivo)
        {
            string caminho = $"{CaminhoSlimes}/{arquivo}";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
            if (sprite == null)
            {
                throw new FileNotFoundException("A imagem de animacao do slime nao foi importada como Sprite.", caminho);
            }

            return sprite;
        }

        private static void GarantirPasta(string caminho)
        {
            if (!AssetDatabase.IsValidFolder(caminho))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
        }
    }
}
