using System.IO;
using Avalonia;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Avalonia.Editor
{
    public static class ConfigurarCombateSlimesAvalonia
    {
        private const string CaminhoCena = "Assets/Scenes/AvalonyaWorld.unity";
        private const string CaminhoPrefab = "Assets/Prefabs/SlimeAvalonia.prefab";
        private const string CaminhoSlimes = "Assets/Art/Sprites/Slime";
        private const int CamadaPersonagens = 8;
        private const float PixelsPorUnidade = 181.81818f;

        [MenuItem("Avalonia/Migracao/Configurar vida e cabeçada do slime")]
        public static void ConfigurarVidaECabecada()
        {
            ConfigurarImportacaoAtaque();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigurarPrefabSlime();
            ConfigurarVidaJogadorNaCena();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Etapa 3B configurada: vida do jogador e cabeçada telegrafada do slime.");
        }

        private static void ConfigurarPrefabSlime()
        {
            if (!File.Exists(CaminhoPrefab))
            {
                throw new FileNotFoundException("Prefab do slime não encontrado.", CaminhoPrefab);
            }

            GameObject raiz = PrefabUtility.LoadPrefabContents(CaminhoPrefab);
            try
            {
                InimigoSlimeAvalonia slime = raiz.GetComponent<InimigoSlimeAvalonia>();
                CharacterController colisor = raiz.GetComponent<CharacterController>();
                if (slime == null || colisor == null)
                {
                    throw new System.InvalidOperationException("Prefab do slime precisa de controlador e script de movimento.");
                }

                raiz.layer = CamadaPersonagens;
                colisor.height = 1.15f;
                colisor.radius = 0.4f;
                colisor.center = new Vector3(0f, 0.575f, 0f);

                SerializedObject serializado = new SerializedObject(slime);
                SerializedProperty ataques = ObterPropriedade(serializado, "quadrosAtaque");
                ataques.arraySize = 2;
                ataques.GetArrayElementAtIndex(0).objectReferenceValue = CarregarSprite("attack_1.png");
                ataques.GetArrayElementAtIndex(1).objectReferenceValue = CarregarSprite("attack_2.png");
                ObterPropriedade(serializado, "quadrosPorSegundoAtaque").floatValue = 2f / 1.2f;
                ObterPropriedade(serializado, "distanciaAcerto").floatValue = 1.75f;
                ObterPropriedade(serializado, "danoAtaque").floatValue = 2f;
                ObterPropriedade(serializado, "duracaoAtaque").floatValue = 1.2f;
                ObterPropriedade(serializado, "intervaloAtaque").floatValue = 1.3f;
                ObterPropriedade(serializado, "velocidadeInvestida").floatValue = 1.5f;
                ObterPropriedade(serializado, "vidaMaxima").floatValue = 4f;
                serializado.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab, out bool sucesso);
                if (!sucesso)
                {
                    throw new IOException("Unity não conseguiu salvar o prefab do slime.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        private static void ConfigurarVidaJogadorNaCena()
        {
            Scene cena = EditorSceneManager.OpenScene(CaminhoCena, OpenSceneMode.Single);
            GameObject objetoJogador = GameObject.Find("JogadorAvalonia");
            if (objetoJogador == null)
            {
                throw new System.InvalidOperationException("JogadorAvalonia não existe na cena.");
            }

            objetoJogador.layer = CamadaPersonagens;
            JogadorAvalonia jogador = objetoJogador.GetComponent<JogadorAvalonia>();
            if (jogador == null)
            {
                throw new System.InvalidOperationException("O controlador do jogador não foi encontrado.");
            }

            VidaJogadorAvalonia vida = objetoJogador.GetComponent<VidaJogadorAvalonia>();
            if (vida == null)
            {
                vida = objetoJogador.AddComponent<VidaJogadorAvalonia>();
            }

            SerializedObject serializado = new SerializedObject(vida);
            ObterPropriedade(serializado, "jogador").objectReferenceValue = jogador;
            ObterPropriedade(serializado, "vidaMaxima").floatValue = 12f;
            serializado.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena, CaminhoCena);
        }

        private static Sprite CarregarSprite(string arquivo)
        {
            string caminho = $"{CaminhoSlimes}/{arquivo}";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
            if (sprite == null)
            {
                throw new FileNotFoundException("Sprite de ataque do slime não foi importado.", caminho);
            }

            return sprite;
        }

        private static SerializedProperty ObterPropriedade(SerializedObject serializado, string nome)
        {
            SerializedProperty propriedade = serializado.FindProperty(nome);
            if (propriedade == null)
            {
                throw new System.InvalidOperationException(
                    $"O campo serializado '{nome}' não existe em {serializado.targetObject.GetType().Name}.");
            }

            return propriedade;
        }

        private static void ConfigurarImportacaoAtaque()
        {
            foreach (string arquivo in new[] { "attack_1.png", "attack_2.png" })
            {
                string caminho = $"{CaminhoSlimes}/{arquivo}";
                TextureImporter importador = AssetImporter.GetAtPath(caminho) as TextureImporter;
                if (importador == null)
                {
                    throw new FileNotFoundException("Importador de sprite do slime não encontrado.", caminho);
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
    }
}
