using System;
using System.IO;
using Avalonia;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Avalonia.Editor
{
    public static class ConfigurarAranhasAvalonia
    {
        private const string CaminhoCena = "Assets/Scenes/AvalonyaWorld.unity";
        private const string CaminhoPrefab = "Assets/Prefabs/AranhaAvalonia.prefab";
        private const string CaminhoPrefabPequena = "Assets/Prefabs/AranhaPequenaAvalonia.prefab";
        private const string PastaOrigem = "Assets/Art/Sprites/Aranha";
        private const string PastaQuadros = "Assets/Art/Generated/Aranha";
        private const string CaminhoRecuo = "Assets/Art/Sprites/Knockback/aranha_knockback.png";
        private const int CamadaPersonagens = 8;
        private const float PixelsPorUnidade = 462.17f;

        [MenuItem("Avalonia/Migracao/Configurar aranha comum e surgimento")]
        public static void ConfigurarAranhaComum()
        {
            GerarQuadrosAlinhados();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigurarImportacaoSprites();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            GameObject prefab = CriarOuAtualizarPrefab();
            GameObject prefabPequena = CriarOuAtualizarPrefabPequeno(prefab);
            ConfigurarCena(prefab, prefabPequena);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Etapa 3D configurada: aranha comum, cabeçada, recuo e surgimento por tempo.");
        }

        private static void GerarQuadrosAlinhados()
        {
            Directory.CreateDirectory(Path.GetFullPath(PastaQuadros));
            var caminhadaEAtaque = new RetanguloAtlas(724, 577, 621, 645, 169, 127, 345, 143);
            var morte = new RetanguloAtlas(1034, 365, 645, 576, 109, 113, 217, 127);

            for (int indice = 1; indice <= 5; indice++)
            {
                RecortarQuadro($"walk_{indice}", caminhadaEAtaque);
            }

            for (int indice = 1; indice <= 2; indice++)
            {
                RecortarQuadro($"attack_{indice}", caminhadaEAtaque);
                RecortarQuadro($"death_{indice}", morte);
            }
        }

        private static void RecortarQuadro(string nome, RetanguloAtlas quadro)
        {
            string origem = $"{PastaOrigem}/{nome}.png";
            string destino = $"{PastaQuadros}/{nome}.png";
            string caminhoOrigem = Path.GetFullPath(origem);
            if (!File.Exists(caminhoOrigem))
            {
                throw new FileNotFoundException("Quadro original da aranha não encontrado.", caminhoOrigem);
            }

            Texture2D atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Texture2D tela = null;
            try
            {
                if (!atlas.LoadImage(File.ReadAllBytes(caminhoOrigem), false))
                {
                    throw new InvalidDataException($"Não foi possível abrir o atlas {origem}.");
                }

                if (quadro.x + quadro.largura > atlas.width || quadro.yTopo + quadro.altura > atlas.height)
                {
                    throw new InvalidDataException($"A região declarada para {nome} ultrapassa o atlas {origem}.");
                }

                int alturaTela = quadro.altura + quadro.margemAltura;
                tela = new Texture2D(
                    quadro.largura + quadro.margemLargura,
                    alturaTela,
                    TextureFormat.RGBA32,
                    false
                );
                tela.SetPixels32(new Color32[tela.width * tela.height]);

                int yAtlas = atlas.height - quadro.yTopo - quadro.altura;
                int yTela = alturaTela - quadro.margemTopo - quadro.altura;
                Color[] pixels = atlas.GetPixels(quadro.x, yAtlas, quadro.largura, quadro.altura);
                tela.SetPixels(quadro.margemEsquerda, yTela, quadro.largura, quadro.altura, pixels);
                tela.Apply(false, false);

                string caminhoDestino = Path.GetFullPath(destino);
                File.WriteAllBytes(caminhoDestino, tela.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(atlas);
                if (tela != null)
                {
                    UnityEngine.Object.DestroyImmediate(tela);
                }
            }
        }

        private static void ConfigurarImportacaoSprites()
        {
            foreach (string arquivo in new[]
                     {
                         "walk_1.png", "walk_2.png", "walk_3.png", "walk_4.png", "walk_5.png",
                         "attack_1.png", "attack_2.png", "death_1.png", "death_2.png"
                     })
            {
                ConfigurarImportacaoComoSprite($"{PastaQuadros}/{arquivo}");
            }

            ConfigurarImportacaoComoSprite(CaminhoRecuo);
        }

        private static void ConfigurarImportacaoComoSprite(string caminho)
        {
            TextureImporter importador = AssetImporter.GetAtPath(caminho) as TextureImporter;
            if (importador == null)
            {
                throw new FileNotFoundException("Importador de sprite da aranha não foi encontrado.", caminho);
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

        private static GameObject CriarOuAtualizarPrefab()
        {
            GameObject raiz = new GameObject("AranhaAvalonia");
            try
            {
                raiz.layer = CamadaPersonagens;
                CharacterController colisor = raiz.AddComponent<CharacterController>();
                colisor.height = 0.68f;
                colisor.radius = 0.29f;
                colisor.center = new Vector3(0f, 0.469f, 0f);
                colisor.skinWidth = 0.04f;
                colisor.stepOffset = 0.1f;
                colisor.minMoveDistance = 0f;

                GameObject objetoVisual = new GameObject("VisualAranha");
                objetoVisual.transform.SetParent(raiz.transform, false);
                objetoVisual.transform.localPosition = new Vector3(0f, 0.63f, 0f);

                SpriteRenderer renderizador = objetoVisual.AddComponent<SpriteRenderer>();
                renderizador.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderizador.receiveShadows = false;
                renderizador.sortingOrder = 11;

                InimigoAranhaAvalonia aranha = raiz.AddComponent<InimigoAranhaAvalonia>();
                SerializedObject serializado = new SerializedObject(aranha);
                DefinirObjeto(serializado, "visual", renderizador);
                DefinirObjeto(serializado, "spriteRecuo", CarregarSprite(CaminhoRecuo));
                DefinirFloat(serializado, "velocidade", 5f);
                DefinirFloat(serializado, "vidaMaxima", 1f);
                DefinirFloat(serializado, "distanciaInicioAtaque", 1.45f);
                DefinirFloat(serializado, "distanciaAcerto", 1.75f);
                DefinirFloat(serializado, "danoAtaque", 1f);
                DefinirFloat(serializado, "duracaoAtaque", 0.25f);
                DefinirFloat(serializado, "intervaloAtaque", 0.25f);
                DefinirFloat(serializado, "velocidadeInvestida", 6f);
                DefinirFloat(serializado, "forcaRecuo", 8f);
                DefinirFloat(serializado, "duracaoRecuo", 0.22f);
                DefinirFloat(serializado, "raioHorizontalDano", 0.4f);
                DefinirFloat(serializado, "quadrosPorSegundoAndar", 4.5f);
                PreencherQuadros(serializado, "quadrosAndar", 5, "walk");
                PreencherQuadros(serializado, "quadrosAtaque", 2, "attack");
                serializado.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab, out bool sucesso);
                if (!sucesso)
                {
                    throw new IOException("Unity não conseguiu salvar o prefab da aranha.");
                }

                return AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefab);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(raiz);
            }
        }

        private static void PreencherQuadros(SerializedObject serializado, string nome, int quantidade, string prefixo)
        {
            SerializedProperty quadros = ObterPropriedade(serializado, nome);
            quadros.arraySize = quantidade;
            for (int indice = 0; indice < quantidade; indice++)
            {
                string caminho = $"{PastaQuadros}/{prefixo}_{indice + 1}.png";
                quadros.GetArrayElementAtIndex(indice).objectReferenceValue = CarregarSprite(caminho);
            }
        }

        private static GameObject CriarOuAtualizarPrefabPequeno(GameObject prefabNormal)
        {
            GameObject instancia = PrefabUtility.InstantiatePrefab(prefabNormal) as GameObject;
            if (instancia == null)
            {
                throw new InvalidOperationException("Unity não conseguiu instanciar o prefab da aranha comum.");
            }

            try
            {
                PrefabUtility.UnpackPrefabInstance(
                    instancia,
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction
                );
                instancia.name = "AranhaPequenaAvalonia";

                Transform visual = instancia.transform.Find("VisualAranha");
                SpriteRenderer renderizador = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
                InimigoAranhaAvalonia aranha = instancia.GetComponent<InimigoAranhaAvalonia>();
                CharacterController colisor = instancia.GetComponent<CharacterController>();
                if (visual == null || renderizador == null || aranha == null || colisor == null)
                {
                    throw new InvalidOperationException("Prefab da aranha comum sem componentes esperados.");
                }

                visual.localScale = Vector3.one * 0.45f;
                renderizador.color = new Color(1.8f, 0.2f, 0.2f, 1f);
                colisor.height = 0.64f;
                colisor.radius = 0.29f;
                colisor.center = new Vector3(0f, 0.454f, 0f);

                SerializedObject serializado = new SerializedObject(aranha);
                DefinirFloat(serializado, "velocidade", 8f);
                DefinirFloat(serializado, "vidaMaxima", 1f);
                DefinirFloat(serializado, "danoAtaque", 5f);
                DefinirFloat(serializado, "duracaoAtaque", 0.25f);
                DefinirFloat(serializado, "intervaloAtaque", 0.25f);
                DefinirFloat(serializado, "velocidadeInvestida", 6f);
                DefinirInt(serializado, "xpPorAbate", 1);
                serializado.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(instancia, CaminhoPrefabPequena, out bool sucesso);
                if (!sucesso)
                {
                    throw new IOException("Unity não conseguiu salvar o prefab da aranha pequena.");
                }

                return AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefabPequena);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instancia);
            }
        }

        private static void ConfigurarCena(GameObject prefab, GameObject prefabPequena)
        {
            Scene cena = EditorSceneManager.OpenScene(CaminhoCena, OpenSceneMode.Single);
            GameObject objetoJogador = GameObject.Find("JogadorAvalonia");
            if (objetoJogador == null || prefab == null)
            {
                throw new InvalidOperationException("O jogador ou o prefab AranhaAvalonia não foi encontrado.");
            }

            GameObject objetoSpawner = GameObject.Find("AranhaSpawn");
            if (objetoSpawner == null)
            {
                objetoSpawner = new GameObject("AranhaSpawn");
                objetoSpawner.transform.SetParent(cena.GetRootGameObjects()[0].transform, false);
            }

            SpawnerAranhaAvalonia spawner = objetoSpawner.GetComponent<SpawnerAranhaAvalonia>();
            if (spawner == null)
            {
                spawner = objetoSpawner.AddComponent<SpawnerAranhaAvalonia>();
            }

            spawner.Configurar(objetoJogador.transform, prefab.GetComponent<InimigoAranhaAvalonia>());
            SerializedObject serializado = new SerializedObject(spawner);
            DefinirFloat(serializado, "raioMinimo", 15f);
            DefinirFloat(serializado, "raioMaximo", 30f);
            DefinirFloat(serializado, "atrasoInicial", 3f);
            DefinirFloatArray(serializado, "inicioEtapas", new[] { 0f, 30f, 60f, 120f, 180f });
            DefinirFloatArray(serializado, "intervalos", new[] { 3f, 2f, 1.5f, 1f, 0.75f });
            DefinirIntArray(serializado, "limites", new[] { 5, 10, 15, 20, 30 });
            DefinirInt(serializado, "maximoAbsoluto", 30);
            serializado.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(cena);

            GameObject objetoEnxame = GameObject.Find("EnxameAranhasSpawn");
            if (objetoEnxame == null)
            {
                throw new InvalidOperationException("O objeto EnxameAranhasSpawn não foi encontrado na cena.");
            }

            SpawnerEnxameAranhasAvalonia enxame = objetoEnxame.GetComponent<SpawnerEnxameAranhasAvalonia>();
            if (enxame == null)
            {
                enxame = objetoEnxame.AddComponent<SpawnerEnxameAranhasAvalonia>();
            }

            enxame.Configurar(
                objetoJogador.transform,
                prefabPequena != null ? prefabPequena.GetComponent<InimigoAranhaAvalonia>() : null
            );
            SerializedObject serializadoEnxame = new SerializedObject(enxame);
            DefinirFloat(serializadoEnxame, "atrasoInicial", 30f);
            DefinirFloat(serializadoEnxame, "aleatoriedadePrimeiraOnda", 30f);
            DefinirFloat(serializadoEnxame, "intervaloOndas", 10f);
            DefinirInt(serializadoEnxame, "tamanhoOndaBase", 12);
            DefinirInt(serializadoEnxame, "tamanhoOndaPorCarta", 4);
            DefinirInt(serializadoEnxame, "tamanhoOndaMaximo", 30);
            DefinirInt(serializadoEnxame, "maximoAbsoluto", 200);
            DefinirFloat(serializadoEnxame, "velocidadeMobs", 8f);
            DefinirFloat(serializadoEnxame, "danoToque", 5f);
            DefinirFloat(serializadoEnxame, "raioMinimo", 15f);
            DefinirFloat(serializadoEnxame, "raioMaximo", 30f);
            DefinirFloat(serializadoEnxame, "dispersao", 4f);
            serializadoEnxame.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena, CaminhoCena);
        }

        private static void DefinirFloat(SerializedObject serializado, string nome, float valor)
        {
            ObterPropriedade(serializado, nome).floatValue = valor;
        }

        private static void DefinirInt(SerializedObject serializado, string nome, int valor)
        {
            ObterPropriedade(serializado, nome).intValue = valor;
        }

        private static void DefinirObjeto(SerializedObject serializado, string nome, UnityEngine.Object valor)
        {
            ObterPropriedade(serializado, nome).objectReferenceValue = valor;
        }

        private static void DefinirFloatArray(SerializedObject serializado, string nome, float[] valores)
        {
            SerializedProperty propriedade = ObterPropriedade(serializado, nome);
            propriedade.arraySize = valores.Length;
            for (int indice = 0; indice < valores.Length; indice++)
            {
                propriedade.GetArrayElementAtIndex(indice).floatValue = valores[indice];
            }
        }

        private static void DefinirIntArray(SerializedObject serializado, string nome, int[] valores)
        {
            SerializedProperty propriedade = ObterPropriedade(serializado, nome);
            propriedade.arraySize = valores.Length;
            for (int indice = 0; indice < valores.Length; indice++)
            {
                propriedade.GetArrayElementAtIndex(indice).intValue = valores[indice];
            }
        }

        private static SerializedProperty ObterPropriedade(SerializedObject serializado, string nome)
        {
            SerializedProperty propriedade = serializado.FindProperty(nome);
            if (propriedade == null)
            {
                throw new InvalidOperationException($"Campo serializado '{nome}' não existe em {serializado.targetObject.GetType().Name}.");
            }

            return propriedade;
        }

        private static Sprite CarregarSprite(string caminho)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
            if (sprite == null)
            {
                throw new FileNotFoundException("Sprite da aranha não foi importado.", caminho);
            }

            return sprite;
        }

        private readonly struct RetanguloAtlas
        {
            public readonly int x;
            public readonly int yTopo;
            public readonly int largura;
            public readonly int altura;
            public readonly int margemEsquerda;
            public readonly int margemTopo;
            public readonly int margemLargura;
            public readonly int margemAltura;

            public RetanguloAtlas(int x, int yTopo, int largura, int altura,
                int margemEsquerda, int margemTopo, int margemLargura, int margemAltura)
            {
                this.x = x;
                this.yTopo = yTopo;
                this.largura = largura;
                this.altura = altura;
                this.margemEsquerda = margemEsquerda;
                this.margemTopo = margemTopo;
                this.margemLargura = margemLargura;
                this.margemAltura = margemAltura;
            }
        }
    }
}
