using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Avalonya.Editor
{
    public static class GeradorCenarioInicial
    {
        private const string CaminhoCena = "Assets/Scenes/AvalonyaWorld.unity";
        private const string CaminhoMapa = "Assets/Art/Environment/BackGroundGround.png";
        private const string CaminhoDadosCenario = "Assets/Editor/Data/cenario-godot.json";
        private const string CaminhoPastaMateriais = "Assets/Art/Materials";
        private const string CaminhoMaterialMapa = "Assets/Art/Materials/MapaAvalonya.mat";

        [MenuItem("Avalonya/Migracao/Gerar cenario base")]
        public static void GerarCenarioBase()
        {
            if (File.Exists(CaminhoCena))
            {
                throw new System.InvalidOperationException(
                    "A cena base ja existe. Use a rotina de ajuste para preservar alteracoes manuais."
                );
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigurarImportacaoMapa();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Texture2D mapa = AssetDatabase.LoadAssetAtPath<Texture2D>(CaminhoMapa);
            if (mapa == null)
            {
                throw new FileNotFoundException("A arte do mapa nao foi importada.", CaminhoMapa);
            }

            CriarPastaSeNecessario(CaminhoPastaMateriais);
            Material materialMapa = CriarMaterialDoMapa(mapa);

            Scene cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject mundo = new GameObject("MundoAvalonya");
            GameObject cenario = CriarGrupo("Cenario", mundo.transform);
            GameObject pontos = CriarGrupo("PontosDeEntrada", mundo.transform);

            ConfigurarIluminacao(cenario.transform);
            CriarMapa(cenario.transform, materialMapa);
            CriarChaoFisico(cenario.transform);
            CriarCamera(pontos.transform);
            CriarPontoDoJogador(pontos.transform);
            CriarPontosDeSurgimento(pontos.transform);

            PlayerSettings.productName = "";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;

            EditorSceneManager.SaveScene(cena, CaminhoCena);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(CaminhoCena, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Etapa 1 pronta: mapa, iluminacao, colisao do chao, camera e pontos de entrada.");
        }

        [MenuItem("Avalonya/Migracao/Ajustar cenario existente")]
        public static void AjustarCenarioExistente()
        {
            ConfigurarImportacaoMapa();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Scene cena = EditorSceneManager.OpenScene(CaminhoCena, OpenSceneMode.Single);
            GameObject chao = GameObject.Find("ChaoFisico");
            BoxCollider colisor = chao != null ? chao.GetComponent<BoxCollider>() : null;
            if (colisor == null)
            {
                throw new System.InvalidOperationException(
                    "O collider ChaoFisico nao foi encontrado na cena existente."
                );
            }

            colisor.size = new Vector3(426.0789f, 0.1f, 426.0789f);
            colisor.center = new Vector3(0f, -0.06f, 0f);
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena, CaminhoCena);
            AssetDatabase.SaveAssets();
            Texture2D mapa = AssetDatabase.LoadAssetAtPath<Texture2D>(CaminhoMapa);
            TextureImporter importador = AssetImporter.GetAtPath(CaminhoMapa) as TextureImporter;
            importador.GetSourceTextureWidthAndHeight(out int larguraFonte, out int alturaFonte);
            Debug.Log(
                $"Etapa 1 ajustada: collider alinhado ao Godot; textura fonte {larguraFonte}x{alturaFonte}, "
                + $"importada {mapa.width}x{mapa.height}."
            );
        }

        [MenuItem("Avalonya/Migracao/Importar objetos do Godot")]
        public static void ImportarObjetosGodot()
        {
            string caminhoJson = Path.Combine(Application.dataPath, "Editor/Data/cenario-godot.json");
            CenarioExportado dados = JsonUtility.FromJson<CenarioExportado>(File.ReadAllText(caminhoJson));
            if (dados == null || dados.sprites == null || dados.boxes == null || dados.colliders == null)
            {
                throw new System.InvalidOperationException("Os dados de cenario exportados estao incompletos.");
            }

            HashSet<string> texturasConfiguradas = new HashSet<string>();
            foreach (SpriteExportado sprite in dados.sprites)
            {
                string caminhoTextura = ConverterCaminhoTextura(sprite.texture);
                if (texturasConfiguradas.Add(caminhoTextura))
                {
                    ConfigurarImportacaoSprite(caminhoTextura);
                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Scene cena = EditorSceneManager.OpenScene(CaminhoCena, OpenSceneMode.Single);
            GameObject mundo = GameObject.Find("MundoAvalonya");
            Transform cenario = mundo != null ? mundo.transform.Find("Cenario") : null;
            if (cenario == null)
            {
                throw new System.InvalidOperationException("O grupo Cenario nao foi encontrado na cena.");
            }

            Transform antigo = cenario.Find("CenarioImportadoGodot");
            if (antigo != null)
            {
                Object.DestroyImmediate(antigo.gameObject);
            }

            GameObject raizImportada = new GameObject("CenarioImportadoGodot");
            raizImportada.transform.SetParent(cenario, false);
            Transform grupoSprites = CriarGrupo("DecoracaoSprite3D", raizImportada.transform).transform;
            Transform grupoObstaculos = CriarGrupo("ObstaculosComColisao", raizImportada.transform).transform;

            int spritesCriados = 0;
            foreach (SpriteExportado item in dados.sprites)
            {
                if (!item.visible)
                {
                    continue;
                }

                string caminhoTextura = ConverterCaminhoTextura(item.texture);
                Sprite spriteAsset = AssetDatabase.LoadAssetAtPath<Sprite>(caminhoTextura);
                if (spriteAsset == null)
                {
                    throw new FileNotFoundException("O sprite convertido nao foi encontrado.", caminhoTextura);
                }

                GameObject objeto = new GameObject(NomeImportado(item.path));
                objeto.transform.SetParent(grupoSprites, false);
                AplicarTransformacao(objeto.transform, item.position, item.right, item.up, item.forward);

                const float pixelsPorUnidade = 100f;
                objeto.transform.localScale = new Vector3(
                    item.pixel_size * Comprimento(item.right) * pixelsPorUnidade,
                    item.pixel_size * Comprimento(item.up) * pixelsPorUnidade,
                    1f
                );

                SpriteRenderer renderizador = objeto.AddComponent<SpriteRenderer>();
                renderizador.sprite = spriteAsset;
                renderizador.shadowCastingMode = ShadowCastingMode.Off;
                renderizador.receiveShadows = false;
                spritesCriados++;
            }

            Dictionary<string, ColliderExportado> colisoresPorCaminho = new Dictionary<string, ColliderExportado>();
            foreach (ColliderExportado colisor in dados.colliders)
            {
                colisoresPorCaminho[colisor.path] = colisor;
            }

            Material materialObstaculo = CriarMaterialDeCor(
                "ObstaculoCenarioGodot",
                new Color(0.145266f, 0.145266f, 0.145266f, 1f)
            );

            int obstaculosCriados = 0;
            foreach (CaixaExportada caixa in dados.boxes)
            {
                string caminhoColisor = caixa.path + "/StaticBody3D/CollisionShape3D";
                if (!colisoresPorCaminho.TryGetValue(caminhoColisor, out ColliderExportado colisor)
                    || colisor.shape != "BoxShape3D")
                {
                    throw new System.InvalidOperationException(
                        $"A caixa '{caixa.path}' nao possui um BoxShape3D correspondente."
                    );
                }

                GameObject objeto = GameObject.CreatePrimitive(PrimitiveType.Cube);
                objeto.name = NomeImportado(caixa.path);
                objeto.transform.SetParent(grupoObstaculos, false);
                AplicarTransformacao(objeto.transform, caixa.position, caixa.right, caixa.up, caixa.forward);
                objeto.transform.localScale = new Vector3(
                    Comprimento(caixa.right) * caixa.size[0],
                    Comprimento(caixa.up) * caixa.size[1],
                    Comprimento(caixa.forward) * caixa.size[2]
                );

                BoxCollider colliderUnity = objeto.GetComponent<BoxCollider>();
                colliderUnity.size = new Vector3(colisor.size[0], colisor.size[1], colisor.size[2]);

                MeshRenderer rendererUnity = objeto.GetComponent<MeshRenderer>();
                rendererUnity.sharedMaterial = materialObstaculo;
                obstaculosCriados++;
            }

            int colisoesCenarioCriadas = 0;
            foreach (ColliderExportado colisor in dados.colliders)
            {
                if (colisor.shape != "BoxShape3D" || !ColisorDeCenarioDecorativo(colisor.path))
                {
                    continue;
                }

                GameObject objeto = new GameObject(NomeImportado(colisor.path));
                objeto.transform.SetParent(grupoObstaculos, false);
                AplicarTransformacao(objeto.transform, colisor.position, colisor.right, colisor.up, colisor.forward);
                objeto.transform.localScale = new Vector3(
                    Comprimento(colisor.right),
                    Comprimento(colisor.up),
                    Comprimento(colisor.forward)
                );

                BoxCollider colliderUnity = objeto.AddComponent<BoxCollider>();
                colliderUnity.size = new Vector3(colisor.size[0], colisor.size[1], colisor.size[2]);
                colisoesCenarioCriadas++;
            }

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena, CaminhoCena);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Cenario Godot importado: {spritesCriados} sprites decorativos e "
                + $"{obstaculosCriados} obstaculos visiveis e {colisoesCenarioCriadas} colisores de aderecos."
            );
        }

        public static void CapturarPreview()
        {
            EditorSceneManager.OpenScene(CaminhoCena, OpenSceneMode.Single);
            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new System.InvalidOperationException("A camera principal nao foi encontrada na cena.");
            }

            const int largura = 1280;
            const int altura = 720;
            RenderTexture renderTexture = new RenderTexture(largura, altura, 24, RenderTextureFormat.ARGB32);
            RenderTexture anterior = RenderTexture.active;
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;

            Texture2D imagem = new Texture2D(largura, altura, TextureFormat.RGB24, false);
            imagem.ReadPixels(new Rect(0, 0, largura, altura), 0, 0);
            imagem.Apply();
            File.WriteAllBytes("/tmp/avalonya-stage1-preview.png", imagem.EncodeToPNG());

            camera.targetTexture = null;
            RenderTexture.active = anterior;
            Object.DestroyImmediate(imagem);
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);
            Debug.Log("Preview visual salvo em /tmp/avalonya-stage1-preview.png.");
        }

        private static void ConfigurarIluminacao(Transform pai)
        {
            GameObject luzObjeto = new GameObject("LuzDirecional");
            luzObjeto.transform.SetParent(pai, false);
            luzObjeto.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            Light luz = luzObjeto.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.color = new Color(1f, 0.89f, 0.73f);
            luz.intensity = 1.25f;
            luz.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.39f, 0.45f, 0.51f);
            RenderSettings.ambientEquatorColor = new Color(0.31f, 0.35f, 0.32f);
            RenderSettings.ambientGroundColor = new Color(0.16f, 0.18f, 0.15f);
            RenderSettings.fog = false;
        }

        private static void CriarMapa(Transform pai, Material materialMapa)
        {
            GameObject plano = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plano.name = "MapaPintado";
            plano.transform.SetParent(pai, false);
            plano.transform.position = new Vector3(0f, 0.015f, 0f);
            plano.transform.localScale = new Vector3(41.04f, 1f, 24.36f);

            MeshCollider colisorDoPlano = plano.GetComponent<MeshCollider>();
            if (colisorDoPlano != null)
            {
                Object.DestroyImmediate(colisorDoPlano);
            }

            MeshRenderer renderizador = plano.GetComponent<MeshRenderer>();
            renderizador.sharedMaterial = materialMapa;
            renderizador.shadowCastingMode = ShadowCastingMode.Off;
            renderizador.receiveShadows = false;
        }

        private static void CriarChaoFisico(Transform pai)
        {
            GameObject chao = new GameObject("ChaoFisico");
            chao.transform.SetParent(pai, false);

            BoxCollider colisor = chao.AddComponent<BoxCollider>();
            colisor.size = new Vector3(426.0789f, 0.1f, 426.0789f);
            colisor.center = new Vector3(0f, -0.06f, 0f);
        }

        private static void CriarCamera(Transform pai)
        {
            GameObject cameraObjeto = new GameObject("CameraPrincipal");
            cameraObjeto.tag = "MainCamera";
            cameraObjeto.transform.SetParent(pai, false);
            cameraObjeto.transform.position = new Vector3(17.86f, 10.73f, 33.73f);
            cameraObjeto.transform.rotation = Quaternion.LookRotation(
                new Vector3(16.36f, 2.56f, 25.42f) - cameraObjeto.transform.position,
                Vector3.up);

            Camera camera = cameraObjeto.AddComponent<Camera>();
            camera.fieldOfView = 75f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1000f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.20f, 0.27f, 0.29f);
            camera.allowHDR = true;
            camera.allowMSAA = true;

            cameraObjeto.AddComponent<AudioListener>();
        }

        private static void CriarPontoDoJogador(Transform pai)
        {
            GameObject ponto = new GameObject("PlayerSpawn");
            ponto.transform.SetParent(pai, false);
            ponto.transform.position = new Vector3(16.3645f, 0f, 25.4164f);

            GameObject marcador = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            marcador.name = "MarcadorTemporarioDoJogador";
            marcador.transform.SetParent(ponto.transform, false);
            marcador.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            marcador.transform.localScale = new Vector3(0.62f, 0.9f, 0.62f);
            Object.DestroyImmediate(marcador.GetComponent<CapsuleCollider>());

            Renderer renderizador = marcador.GetComponent<Renderer>();
            renderizador.sharedMaterial = CriarMaterialDeCor("MarcadorTemporario", new Color(0.76f, 0.34f, 0.19f));
        }

        private static void CriarPontosDeSurgimento(Transform pai)
        {
            Transform grupo = CriarGrupo("Spawners", pai).transform;
            CriarPonto("SlimeSpawn", new Vector3(-8f, 0f, -12f), grupo);
            CriarPonto("AranhaSpawn", new Vector3(40f, 0f, 15f), grupo);
            CriarPonto("EnxameAranhasSpawn", new Vector3(-26f, 0f, 36f), grupo);
        }

        private static void CriarPonto(string nome, Vector3 posicao, Transform pai)
        {
            GameObject ponto = new GameObject(nome);
            ponto.transform.SetParent(pai, false);
            ponto.transform.position = posicao;
        }

        private static Material CriarMaterialDoMapa(Texture2D mapa)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                throw new System.InvalidOperationException("O shader URP Unlit nao foi encontrado.");
            }

            Material material = new Material(shader)
            {
                name = "MapaAvalonya"
            };
            material.SetTexture("_BaseMap", mapa);
            material.SetColor("_BaseColor", Color.white);

            Material materialExistente = AssetDatabase.LoadAssetAtPath<Material>(CaminhoMaterialMapa);
            if (materialExistente != null)
            {
                EditorUtility.CopySerialized(material, materialExistente);
                Object.DestroyImmediate(material);
                return materialExistente;
            }

            AssetDatabase.CreateAsset(material, CaminhoMaterialMapa);
            return material;
        }

        private static void ConfigurarImportacaoMapa()
        {
            TextureImporter importador = AssetImporter.GetAtPath(CaminhoMapa) as TextureImporter;
            if (importador == null)
            {
                throw new FileNotFoundException("O importador do mapa nao foi encontrado.", CaminhoMapa);
            }

            TextureImporterPlatformSettings standalone = importador.GetPlatformTextureSettings("Standalone");
            bool alterado = importador.maxTextureSize != 4096
                || importador.textureCompression != TextureImporterCompression.Uncompressed
                || !importador.mipmapEnabled
                || importador.wrapMode != TextureWrapMode.Clamp
                || importador.npotScale != TextureImporterNPOTScale.None
                || !standalone.overridden
                || standalone.maxTextureSize != 4096
                || standalone.textureCompression != TextureImporterCompression.Uncompressed;

            if (!alterado)
            {
                return;
            }

            importador.maxTextureSize = 4096;
            importador.textureCompression = TextureImporterCompression.Uncompressed;
            importador.mipmapEnabled = true;
            importador.wrapMode = TextureWrapMode.Clamp;
            importador.npotScale = TextureImporterNPOTScale.None;

            TextureImporterPlatformSettings padrao = importador.GetPlatformTextureSettings("DefaultTexturePlatform");
            padrao.name = "DefaultTexturePlatform";
            padrao.overridden = true;
            padrao.maxTextureSize = 4096;
            padrao.textureCompression = TextureImporterCompression.Uncompressed;
            importador.SetPlatformTextureSettings(padrao);

            standalone.name = "Standalone";
            standalone.overridden = true;
            standalone.maxTextureSize = 4096;
            standalone.textureCompression = TextureImporterCompression.Uncompressed;
            importador.SetPlatformTextureSettings(standalone);
            importador.SaveAndReimport();
        }

        private static void ConfigurarImportacaoSprite(string caminho)
        {
            TextureImporter importador = AssetImporter.GetAtPath(caminho) as TextureImporter;
            if (importador == null)
            {
                throw new FileNotFoundException("A textura ambiental nao foi encontrada.", caminho);
            }

            bool alterado = importador.textureType != TextureImporterType.Sprite
                || importador.spriteImportMode != SpriteImportMode.Single
                || importador.spritePixelsPerUnit != 100f
                || importador.maxTextureSize != 4096
                || importador.textureCompression != TextureImporterCompression.Uncompressed
                || importador.npotScale != TextureImporterNPOTScale.None
                || !importador.mipmapEnabled
                || !importador.alphaIsTransparency;

            if (!alterado)
            {
                return;
            }

            importador.textureType = TextureImporterType.Sprite;
            importador.spriteImportMode = SpriteImportMode.Single;
            importador.spritePixelsPerUnit = 100f;
            importador.maxTextureSize = 4096;
            importador.textureCompression = TextureImporterCompression.Uncompressed;
            importador.npotScale = TextureImporterNPOTScale.None;
            importador.mipmapEnabled = true;
            importador.alphaIsTransparency = true;
            importador.wrapMode = TextureWrapMode.Clamp;

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

        private static string ConverterCaminhoTextura(string caminho)
        {
            const string prefixo = "res://Sprites/";
            if (!caminho.StartsWith(prefixo))
            {
                throw new System.InvalidOperationException($"Caminho de sprite inesperado: {caminho}");
            }

            return "Assets/Art/Sprites/" + caminho.Substring(prefixo.Length);
        }

        private static string NomeImportado(string caminho)
        {
            const string prefixo = "MundoTeste/";
            string nome = caminho.StartsWith(prefixo) ? caminho.Substring(prefixo.Length) : caminho;
            return nome.Replace('/', '_');
        }

        private static void AplicarTransformacao(
            Transform destino,
            float[] posicao,
            float[] direita,
            float[] cima,
            float[] frente
        )
        {
            destino.position = new Vector3(posicao[0], posicao[1], posicao[2]);
            destino.rotation = Quaternion.LookRotation(
                new Vector3(frente[0], frente[1], frente[2]),
                new Vector3(cima[0], cima[1], cima[2])
            );
        }

        private static float Comprimento(float[] vetor)
        {
            return new Vector3(vetor[0], vetor[1], vetor[2]).magnitude;
        }

        private static bool ColisorDeCenarioDecorativo(string caminho)
        {
            return caminho.StartsWith("MundoTeste/CenarioPT20/")
                || caminho.StartsWith("MundoTeste/CenarioPT21/")
                || caminho.StartsWith("MundoTeste/CenarioPT22/")
                || caminho.StartsWith("MundoTeste/CenarioPT23/");
        }

        [System.Serializable]
        private sealed class CenarioExportado
        {
            public SpriteExportado[] sprites;
            public CaixaExportada[] boxes;
            public ColliderExportado[] colliders;
        }

        [System.Serializable]
        private sealed class SpriteExportado
        {
            public string name;
            public string path;
            public string texture;
            public float[] position;
            public float[] right;
            public float[] up;
            public float[] forward;
            public float pixel_size;
            public bool visible;
        }

        [System.Serializable]
        private sealed class CaixaExportada
        {
            public string name;
            public string path;
            public float[] position;
            public float[] right;
            public float[] up;
            public float[] forward;
            public float[] basis_lengths;
            public float[] size;
            public float[] color;
            public bool visible;
        }

        [System.Serializable]
        private sealed class ColliderExportado
        {
            public string name;
            public string path;
            public string shape;
            public float[] position;
            public float[] right;
            public float[] up;
            public float[] forward;
            public float[] size;
        }

        private static Material CriarMaterialDeCor(string nome, Color cor)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Material material = new Material(shader)
            {
                name = nome,
                color = cor
            };

            string caminho = $"{CaminhoPastaMateriais}/{nome}.mat";
            Material existente = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            if (existente != null)
            {
                EditorUtility.CopySerialized(material, existente);
                Object.DestroyImmediate(material);
                return existente;
            }

            AssetDatabase.CreateAsset(material, caminho);
            return material;
        }

        private static GameObject CriarGrupo(string nome, Transform pai)
        {
            GameObject grupo = new GameObject(nome);
            grupo.transform.SetParent(pai, false);
            return grupo;
        }

        private static void CriarPastaSeNecessario(string caminho)
        {
            if (!AssetDatabase.IsValidFolder(caminho))
            {
                string pastaPai = Path.GetDirectoryName(caminho).Replace('\\', '/');
                string nome = Path.GetFileName(caminho);
                AssetDatabase.CreateFolder(pastaPai, nome);
            }
        }
    }
}
