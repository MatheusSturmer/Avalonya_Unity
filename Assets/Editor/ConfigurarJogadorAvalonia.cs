using System.IO;
using Avalonia;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Avalonia.Editor
{
    public static class ConfigurarJogadorAvalonia
    {
        private const string CaminhoCena = "Assets/Scenes/AvalonyaWorld.unity";
        private const string PastaSprites = "Assets/Art/Sprites";

        [MenuItem("Avalonia/Migracao/Configurar personagem e camera")]
        public static void ConfigurarPersonagemECamera()
        {
            ConfigurarImportacaoSprites();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Scene cena = EditorSceneManager.OpenScene(CaminhoCena, OpenSceneMode.Single);
            GameObject ponto = GameObject.Find("PlayerSpawn");
            GameObject cameraObjeto = GameObject.Find("CameraPrincipal");
            if (ponto == null || cameraObjeto == null)
            {
                throw new System.InvalidOperationException("PlayerSpawn ou CameraPrincipal nao existe na cena.");
            }

            Transform marcadorAntigo = ponto.transform.Find("MarcadorTemporarioDoJogador");
            if (marcadorAntigo != null)
            {
                Object.DestroyImmediate(marcadorAntigo.gameObject);
            }

            ponto.transform.position = new Vector3(16.3645f, 0f, 25.4164f);
            Transform jogadorExistente = ponto.transform.Find("JogadorAvalonia");
            GameObject jogadorObjeto = jogadorExistente != null
                ? jogadorExistente.gameObject
                : new GameObject("JogadorAvalonia");
            jogadorObjeto.transform.SetParent(ponto.transform, false);
            jogadorObjeto.transform.localPosition = Vector3.zero;
            jogadorObjeto.transform.localRotation = Quaternion.identity;
            jogadorObjeto.transform.localScale = Vector3.one;

            CharacterController colisor = jogadorObjeto.GetComponent<CharacterController>();
            if (colisor == null)
            {
                colisor = jogadorObjeto.AddComponent<CharacterController>();
            }

            colisor.height = 1.8f;
            colisor.radius = 0.32f;
            colisor.center = new Vector3(0f, 0.9f, 0f);
            colisor.skinWidth = 0.08f;
            colisor.stepOffset = 0.2f;
            colisor.slopeLimit = 45f;
            colisor.minMoveDistance = 0f;

            JogadorAvalonia jogador = jogadorObjeto.GetComponent<JogadorAvalonia>();
            if (jogador == null)
            {
                jogador = jogadorObjeto.AddComponent<JogadorAvalonia>();
            }

            Transform visualExistente = jogadorObjeto.transform.Find("VisualJogador");
            GameObject visualObjeto = visualExistente != null
                ? visualExistente.gameObject
                : new GameObject("VisualJogador");
            visualObjeto.transform.SetParent(jogadorObjeto.transform, false);
            visualObjeto.transform.localPosition = new Vector3(0f, 1.152f, 0f);
            visualObjeto.transform.localRotation = Quaternion.identity;
            visualObjeto.transform.localScale = Vector3.one;

            SpriteRenderer renderizador = visualObjeto.GetComponent<SpriteRenderer>();
            if (renderizador == null)
            {
                renderizador = visualObjeto.AddComponent<SpriteRenderer>();
            }

            renderizador.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderizador.receiveShadows = false;
            renderizador.sortingOrder = 10;

            AnimacaoJogadorAvalonia animacao = visualObjeto.GetComponent<AnimacaoJogadorAvalonia>();
            if (animacao == null)
            {
                animacao = visualObjeto.AddComponent<AnimacaoJogadorAvalonia>();
            }

            animacao.spriteFrente = CarregarTextura("FRENTE_sprite_sheet.png");
            animacao.spriteCostas = CarregarTextura("COSTAS_sprite_sheet.png");
            animacao.spriteLadoEsquerdo = CarregarTextura("LADO_ESQUERDO_sprite_sheet.png");
            animacao.spriteLadoDireito = CarregarTextura("LADO_DIREITO_sprite_sheet.png");
            animacao.correrFrente1 = CarregarTextura("Run/run_down_1.png");
            animacao.correrFrente2 = CarregarTextura("Run/run_down_2.png");
            animacao.correrCostas1 = CarregarTextura("Run/run_up_1.png");
            animacao.correrCostas2 = CarregarTextura("Run/run_up_2.png");
            animacao.correrLado1 = CarregarTextura("Run/run_right_1.png");
            animacao.correrLado2 = CarregarTextura("Run/run_right_2.png");
            animacao.danoFrente = CarregarTextura("Dano/hit_down.png");
            animacao.danoCostas = CarregarTextura("Dano/hit_up.png");
            animacao.danoLado = CarregarTextura("Dano/hit_side.png");

            Camera camera = cameraObjeto.GetComponent<Camera>();
            if (camera == null)
            {
                throw new System.InvalidOperationException("CameraPrincipal nao tem componente Camera.");
            }

            cameraObjeto.transform.position = jogadorObjeto.transform.position + new Vector3(1.5f, 8.67f, 8.31f);
            cameraObjeto.transform.rotation = Quaternion.LookRotation(
                new Vector3(0f, -0.69250757f, -0.7214106f),
                Vector3.up
            );

            CameraFollowAvalonia acompanhamento = cameraObjeto.GetComponent<CameraFollowAvalonia>();
            if (acompanhamento == null)
            {
                acompanhamento = cameraObjeto.gameObject.AddComponent<CameraFollowAvalonia>();
            }

            acompanhamento.jogador = jogador;

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena, CaminhoCena);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Etapa 2 configurada: controle X/Z, CharacterController, 4 direcoes de sprites, "
                + "recuo/preparacao de dano e camera com acompanhamento por mola."
            );
        }

        private static Texture2D CarregarTextura(string caminhoRelativo)
        {
            string caminho = $"{PastaSprites}/{caminhoRelativo}";
            Texture2D textura = AssetDatabase.LoadAssetAtPath<Texture2D>(caminho);
            if (textura == null)
            {
                throw new FileNotFoundException("Uma textura do jogador nao foi importada.", caminho);
            }

            return textura;
        }

        private static void ConfigurarImportacaoSprites()
        {
            string[] caminhos =
            {
                "FRENTE_sprite_sheet.png",
                "COSTAS_sprite_sheet.png",
                "LADO_ESQUERDO_sprite_sheet.png",
                "LADO_DIREITO_sprite_sheet.png",
                "Run/run_down_1.png",
                "Run/run_down_2.png",
                "Run/run_up_1.png",
                "Run/run_up_2.png",
                "Run/run_right_1.png",
                "Run/run_right_2.png",
                "Dano/hit_down.png",
                "Dano/hit_up.png",
                "Dano/hit_side.png"
            };

            foreach (string relativo in caminhos)
            {
                string caminho = $"{PastaSprites}/{relativo}";
                TextureImporter importador = AssetImporter.GetAtPath(caminho) as TextureImporter;
                if (importador == null)
                {
                    throw new FileNotFoundException("O importador de uma textura do jogador nao foi encontrado.", caminho);
                }

                bool alterado = importador.textureType != TextureImporterType.Sprite
                    || importador.spriteImportMode != SpriteImportMode.Single
                    || importador.spritePixelsPerUnit != 333.3333f
                    || importador.maxTextureSize != 4096
                    || importador.textureCompression != TextureImporterCompression.Uncompressed
                    || importador.npotScale != TextureImporterNPOTScale.None
                    || !importador.alphaIsTransparency
                    || !importador.isReadable
                    || importador.filterMode != FilterMode.Point
                    || importador.mipmapEnabled;
                if (!alterado)
                {
                    continue;
                }

                importador.textureType = TextureImporterType.Sprite;
                importador.spriteImportMode = SpriteImportMode.Single;
                importador.spritePixelsPerUnit = 333.3333f;
                importador.maxTextureSize = 4096;
                importador.textureCompression = TextureImporterCompression.Uncompressed;
                importador.npotScale = TextureImporterNPOTScale.None;
                importador.alphaIsTransparency = true;
                importador.isReadable = true;
                importador.filterMode = FilterMode.Point;
                importador.mipmapEnabled = false;

                TextureImporterPlatformSettings padrao = importador.GetPlatformTextureSettings("DefaultTexturePlatform");
                padrao.name = "DefaultTexturePlatform";
                padrao.overridden = true;
                padrao.maxTextureSize = 4096;
                padrao.textureCompression = TextureImporterCompression.Uncompressed;
                importador.SetPlatformTextureSettings(padrao);

                TextureImporterPlatformSettings standalone = importador.GetPlatformTextureSettings("Standalone");
                standalone.name = "Standalone";
                standalone.overridden = true;
                standalone.maxTextureSize = 4096;
                standalone.textureCompression = TextureImporterCompression.Uncompressed;
                importador.SetPlatformTextureSettings(standalone);
                importador.SaveAndReimport();
            }
        }
    }
}
