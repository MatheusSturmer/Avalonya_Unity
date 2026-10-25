using System.IO;
using Avalonia;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Avalonia.Editor
{
    public static class ConfigurarAtaqueBasicoAvalonia
    {
        private const string CaminhoCena = "Assets/Scenes/AvalonyaWorld.unity";
        private const string CaminhoPrefab = "Assets/Prefabs/SlimeAvalonia.prefab";
        private const string CaminhoAtaque = "Assets/Art/Sprites/ataque_basico.png";
        private const string CaminhoRecuo = "Assets/Art/Sprites/Knockback/slime_knockback.png";
        private const float PixelsPorUnidade = 181.81818f;

        [MenuItem("Avalonia/Migracao/Configurar ataque basico e recuo do slime")]
        public static void Configurar()
        {
            ConfigurarImportacao(CaminhoAtaque);
            ConfigurarImportacao(CaminhoRecuo);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigurarPrefabSlime();
            ConfigurarJogadorNaCena();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Etapa 3C configurada: ataque basico, vida e recuo dos slimes.");
        }

        private static void ConfigurarPrefabSlime()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(CaminhoPrefab);
            try
            {
                InimigoSlimeAvalonia slime = raiz.GetComponent<InimigoSlimeAvalonia>();
                if (slime == null)
                {
                    throw new System.InvalidOperationException("O prefab do slime nao possui InimigoSlimeAvalonia.");
                }

                SerializedObject serializado = new SerializedObject(slime);
                ObterPropriedade(serializado, "vidaMaxima").floatValue = 4f;
                ObterPropriedade(serializado, "spriteRecuo").objectReferenceValue = CarregarSprite(CaminhoRecuo);
                ObterPropriedade(serializado, "forcaRecuo").floatValue = 8f;
                ObterPropriedade(serializado, "duracaoRecuo").floatValue = 0.22f;
                ObterPropriedade(serializado, "raioHorizontalDano").floatValue = 0.66f;
                serializado.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab, out bool sucesso);
                if (!sucesso)
                {
                    throw new IOException("Unity nao conseguiu salvar o prefab com o recuo do slime.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        private static void ConfigurarJogadorNaCena()
        {
            Scene cena = EditorSceneManager.OpenScene(CaminhoCena, OpenSceneMode.Single);
            GameObject jogadorObjeto = GameObject.Find("JogadorAvalonia");
            if (jogadorObjeto == null)
            {
                throw new System.InvalidOperationException("O objeto JogadorAvalonia nao existe na cena.");
            }

            AtaqueBasicoAvalonia ataque = jogadorObjeto.GetComponent<AtaqueBasicoAvalonia>();
            if (ataque == null)
            {
                ataque = jogadorObjeto.AddComponent<AtaqueBasicoAvalonia>();
            }

            SerializedObject serializado = new SerializedObject(ataque);
            ObterPropriedade(serializado, "dano").floatValue = 2f;
            ObterPropriedade(serializado, "alcance").floatValue = 7f;
            ObterPropriedade(serializado, "aberturaGraus").floatValue = 45f;
            ObterPropriedade(serializado, "intervalo").floatValue = 1.6f;
            ObterPropriedade(serializado, "alturaAtaque").floatValue = 0.65f;
            ObterPropriedade(serializado, "alturaGuia").floatValue = 0.35f;
            ObterPropriedade(serializado, "duracaoEfeito").floatValue = 0.18f;
            ObterPropriedade(serializado, "spriteGolpe").objectReferenceValue = CarregarSprite(CaminhoAtaque);
            ObterPropriedade(serializado, "mascaraObstaculos").intValue = 1;
            serializado.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena, CaminhoCena);
        }

        private static void ConfigurarImportacao(string caminho)
        {
            TextureImporter importador = AssetImporter.GetAtPath(caminho) as TextureImporter;
            if (importador == null)
            {
                throw new FileNotFoundException("A textura do combate nao foi importada.", caminho);
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

        private static Sprite CarregarSprite(string caminho)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
            if (sprite == null)
            {
                throw new FileNotFoundException("A imagem de combate nao foi importada como Sprite.", caminho);
            }

            return sprite;
        }

        private static SerializedProperty ObterPropriedade(SerializedObject serializado, string nome)
        {
            SerializedProperty propriedade = serializado.FindProperty(nome);
            if (propriedade == null)
            {
                throw new System.InvalidOperationException(
                    $"O campo serializado '{nome}' nao existe em {serializado.targetObject.GetType().Name}.");
            }

            return propriedade;
        }
    }
}
