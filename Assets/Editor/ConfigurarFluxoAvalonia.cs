using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Avalonia.Editor
{
    /// <summary>Cria o menu inicial e liga pausa, derrota, reinício e áudio às cenas.</summary>
    public static class ConfigurarFluxoAvalonia
    {
        private const string CaminhoMenu = "Assets/Scenes/MenuAvalonya.unity";
        private const string CaminhoJogo = "Assets/Scenes/AvalonyaWorld.unity";

        [MenuItem("Avalonia/Migracao/Configurar menu, pausa e audio")]
        public static void Configurar()
        {
            AssetDatabase.Refresh();
            ConfigurarMenu();
            ConfigurarCenaDeJogo();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(CaminhoMenu, true),
                new EditorBuildSettingsScene(CaminhoJogo, true)
            };
            EditorSceneManager.OpenScene(CaminhoMenu, OpenSceneMode.Single);
            Debug.Log("Fluxo configurado: menu inicial, opções, pausa, derrota, áudio e cenas de build.");
        }

        private static void ConfigurarMenu()
        {
            Scene cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject raiz = new GameObject(
                "MenuAvalonya",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MenuPrincipalAvalonia), typeof(AudioSource)
            );
            SceneManager.MoveGameObjectToScene(raiz, cena);
            Canvas canvas = raiz.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler escalador = raiz.GetComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);
            escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            escalador.matchWidthOrHeight = 0.5f;
            GameObject cameraObjeto = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            SceneManager.MoveGameObjectToScene(cameraObjeto, cena);
            cameraObjeto.tag = "MainCamera";
            cameraObjeto.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObjeto.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.02f, 0.045f, 1f);
            ConfigurarSistemaDeEventos(cena);

            Font fonte = RecursosFonte();
            PrepararArteDoMenu();
            Sprite fundoOriginal = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Menu/ImagemMenu.jpg");
            Image fundo = CriarImagem(raiz.transform, "ArteOriginalMenu", fundoOriginal, Color.white, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            fundo.preserveAspect = false;
            fundo.raycastTarget = false;

            Image sombra = CriarImagem(raiz.transform, "SombraDaCena", null, new Color(0.015f, 0.02f, 0.045f, 0.18f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            sombra.raycastTarget = false;

            GameObject painelPrincipal = CriarPainel(raiz.transform, "PainelPrincipal", new Vector2(58f, 66f), new Vector2(520f, 620f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Color(0.025f, 0.035f, 0.065f, 0.88f));
            CriarTexto(painelPrincipal.transform, "TituloMenu", "MENU PRINCIPAL", fonte, 32, TextAnchor.MiddleCenter, new Vector2(0f, 200f), new Vector2(440f, 62f)).color = new Color(1f, 0.88f, 0.57f, 1f);
            Button jogar = CriarBotao(painelPrincipal.transform, "BotaoJogar", "JOGAR", fonte, new Vector2(0f, 90f));
            Button opcoes = CriarBotao(painelPrincipal.transform, "BotaoOpcoes", "OPÇÕES", fonte, Vector2.zero);
            Button creditos = CriarBotao(painelPrincipal.transform, "BotaoCreditos", "CRÉDITOS", fonte, new Vector2(0f, -90f));
            Button sair = CriarBotao(painelPrincipal.transform, "BotaoSair", "SAIR", fonte, new Vector2(0f, -180f));

            GameObject painelOpcoes = CriarPainel(raiz.transform, "PainelOpcoes", new Vector2(58f, 66f), new Vector2(520f, 620f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Color(0.025f, 0.035f, 0.065f, 0.94f));
            CriarTexto(painelOpcoes.transform, "TituloOpcoes", "OPÇÕES", fonte, 32, TextAnchor.MiddleCenter, new Vector2(0f, 200f), new Vector2(440f, 62f)).color = new Color(1f, 0.88f, 0.57f, 1f);
            CriarTexto(painelOpcoes.transform, "RotuloVolume", "VOLUME GERAL", fonte, 20, TextAnchor.MiddleLeft, new Vector2(0f, 70f), new Vector2(400f, 38f));
            Slider volume = CriarSlider(painelOpcoes.transform, fonte, new Vector2(0f, 15f));
            Toggle telaCheia = CriarToggle(painelOpcoes.transform, fonte, new Vector2(0f, -80f), "TELA CHEIA");
            Button voltarOpcoes = CriarBotao(painelOpcoes.transform, "BotaoVoltarOpcoes", "VOLTAR", fonte, new Vector2(0f, -180f));

            GameObject painelCreditos = CriarPainel(raiz.transform, "PainelCreditos", new Vector2(58f, 66f), new Vector2(520f, 620f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Color(0.025f, 0.035f, 0.065f, 0.94f));
            CriarTexto(painelCreditos.transform, "TituloCreditos", "CRÉDITOS", fonte, 32, TextAnchor.MiddleCenter, new Vector2(0f, 200f), new Vector2(440f, 62f)).color = new Color(1f, 0.88f, 0.57f, 1f);
            Text corpoCreditos = CriarTexto(
                painelCreditos.transform,
                "TextoCreditos",
                "AVALONYA\n\nProjeto original em Godot,\nem migração para Unity.\n\nArte e áudio foram preservados\na partir dos arquivos do projeto.",
                fonte,
                18,
                TextAnchor.MiddleCenter,
                new Vector2(0f, 35f),
                new Vector2(430f, 250f)
            );
            corpoCreditos.color = new Color(0.9f, 0.92f, 0.97f, 1f);
            Button voltarCreditos = CriarBotao(painelCreditos.transform, "BotaoVoltarCreditos", "VOLTAR", fonte, new Vector2(0f, -225f));

            painelOpcoes.SetActive(false);
            painelCreditos.SetActive(false);

            AudioSource musica = raiz.GetComponent<AudioSource>();
            musica.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/MenuAvalonya.wav");
            musica.loop = true;
            musica.spatialBlend = 0f;
            musica.playOnAwake = false;
            musica.volume = 0.16f;

            MenuPrincipalAvalonia controlador = raiz.GetComponent<MenuPrincipalAvalonia>();
            SerializedObject serializado = new SerializedObject(controlador);
            Atribuir(serializado, "painelPrincipal", painelPrincipal);
            Atribuir(serializado, "painelOpcoes", painelOpcoes);
            Atribuir(serializado, "painelCreditos", painelCreditos);
            Atribuir(serializado, "botaoJogar", jogar);
            Atribuir(serializado, "botaoOpcoes", opcoes);
            Atribuir(serializado, "botaoCreditos", creditos);
            Atribuir(serializado, "botaoSair", sair);
            Atribuir(serializado, "botaoVoltarOpcoes", voltarOpcoes);
            Atribuir(serializado, "botaoVoltarCreditos", voltarCreditos);
            Atribuir(serializado, "controleVolume", volume);
            Atribuir(serializado, "controleTelaCheia", telaCheia);
            Atribuir(serializado, "musicaMenu", musica);
            serializado.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(cena, CaminhoMenu);
        }

        private static void ConfigurarCenaDeJogo()
        {
            Scene cena = EditorSceneManager.OpenScene(CaminhoJogo, OpenSceneMode.Single);
            RemoverObjetoGerado(cena, "Fluxo de Partida Avalonya");
            RemoverObjetoGerado(cena, "Audio da Partida");
            ConfigurarSistemaDeEventos(cena);
            Font fonte = RecursosFonte();

            GameObject raiz = new GameObject("Fluxo de Partida Avalonya", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(FluxoPartidaAvalonia));
            SceneManager.MoveGameObjectToScene(raiz, cena);
            Canvas canvas = raiz.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            CanvasScaler escalador = raiz.GetComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);
            escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            escalador.matchWidthOrHeight = 0.5f;

            GameObject painel = CriarImagem(raiz.transform, "PainelFluxo", null, new Color(0.02f, 0.025f, 0.05f, 0.94f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f)).gameObject;
            GameObject caixa = CriarPainel(painel.transform, "CaixaFluxo", Vector2.zero, new Vector2(620f, 620f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.065f, 0.08f, 0.13f, 0.98f));
            Text titulo = CriarTexto(caixa.transform, "TituloFluxo", "JOGO PAUSADO", fonte, 34, TextAnchor.MiddleCenter, new Vector2(0f, 235f), new Vector2(530f, 70f));
            titulo.color = new Color(1f, 0.88f, 0.57f, 1f);
            Button continuar = CriarBotao(caixa.transform, "BotaoContinuar", "CONTINUAR", fonte, new Vector2(0f, 125f));
            Button reiniciar = CriarBotao(caixa.transform, "BotaoReiniciar", "REINICIAR PARTIDA", fonte, new Vector2(0f, 35f));
            Button menu = CriarBotao(caixa.transform, "BotaoMenu", "RETORNAR AO MENU", fonte, new Vector2(0f, -55f));
            Button sair = CriarBotao(caixa.transform, "BotaoSair", "SAIR", fonte, new Vector2(0f, -145f));
            Text dica = CriarTexto(caixa.transform, "DicaFluxo", "ESC  •  PAUSAR / CONTINUAR", fonte, 16, TextAnchor.MiddleCenter, new Vector2(0f, -240f), new Vector2(520f, 36f));
            dica.color = new Color(0.72f, 0.77f, 0.86f, 1f);
            painel.SetActive(false);

            FluxoPartidaAvalonia fluxo = raiz.GetComponent<FluxoPartidaAvalonia>();
            SerializedObject serializedFluxo = new SerializedObject(fluxo);
            Atribuir(serializedFluxo, "painelFluxo", painel);
            Atribuir(serializedFluxo, "textoTitulo", titulo);
            Atribuir(serializedFluxo, "textoDica", dica);
            Atribuir(serializedFluxo, "botaoContinuar", continuar);
            Atribuir(serializedFluxo, "botaoReiniciar", reiniciar);
            Atribuir(serializedFluxo, "botaoMenu", menu);
            Atribuir(serializedFluxo, "botaoSair", sair);
            serializedFluxo.ApplyModifiedPropertiesWithoutUndo();

            GameObject audioObjeto = new GameObject("Audio da Partida", typeof(AudioJogoAvalonia));
            SceneManager.MoveGameObjectToScene(audioObjeto, cena);
            AudioSource inicial = audioObjeto.AddComponent<AudioSource>();
            inicial.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/LVL1.ogg");
            inicial.loop = true;
            inicial.spatialBlend = 0f;
            inicial.playOnAwake = false;
            AudioSource frenetica = audioObjeto.AddComponent<AudioSource>();
            frenetica.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/LVL2.ogg");
            frenetica.loop = true;
            frenetica.spatialBlend = 0f;
            frenetica.playOnAwake = false;

            AudioJogoAvalonia audio = audioObjeto.GetComponent<AudioJogoAvalonia>();
            SerializedObject serializedAudio = new SerializedObject(audio);
            Atribuir(serializedAudio, "musicaInicial", inicial);
            Atribuir(serializedAudio, "musicaFrenetica", frenetica);
            serializedAudio.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
        }

        private static void ConfigurarSistemaDeEventos(Scene cena)
        {
            EventSystem sistema = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (sistema == null)
            {
                GameObject objeto = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(objeto, cena);
                return;
            }

            if (sistema.GetComponent<InputSystemUIInputModule>() == null)
            {
                BaseInputModule antigo = sistema.GetComponent<BaseInputModule>();
                if (antigo != null)
                {
                    UnityEngine.Object.DestroyImmediate(antigo);
                }

                sistema.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private static GameObject CriarPainel(Transform pai, string nome, Vector2 posicao, Vector2 dimensao, Vector2 ancoraMin, Vector2 ancoraMax, Vector2 pivo, Color cor)
        {
            GameObject painel = CriarRetangulo(pai, nome, posicao, dimensao, ancoraMin, ancoraMax, pivo);
            Image imagem = painel.AddComponent<Image>();
            imagem.color = cor;
            Outline contorno = painel.AddComponent<Outline>();
            contorno.effectColor = new Color(0.67f, 0.48f, 0.25f, 0.82f);
            contorno.effectDistance = new Vector2(2f, -2f);
            return painel;
        }

        private static Image CriarImagem(Transform pai, string nome, Sprite sprite, Color cor, Vector2 posicao, Vector2 dimensao, Vector2 ancoraMin, Vector2 ancoraMax, Vector2 pivo)
        {
            GameObject objeto = CriarRetangulo(pai, nome, posicao, dimensao, ancoraMin, ancoraMax, pivo);
            Image imagem = objeto.AddComponent<Image>();
            imagem.sprite = sprite;
            imagem.color = cor;
            return imagem;
        }

        private static GameObject CriarRetangulo(Transform pai, string nome, Vector2 posicao, Vector2 dimensao, Vector2 ancoraMin, Vector2 ancoraMax, Vector2 pivo)
        {
            GameObject objeto = new GameObject(nome, typeof(RectTransform));
            objeto.transform.SetParent(pai, false);
            RectTransform retangulo = objeto.GetComponent<RectTransform>();
            retangulo.anchorMin = ancoraMin;
            retangulo.anchorMax = ancoraMax;
            retangulo.pivot = pivo;
            retangulo.anchoredPosition = posicao;
            retangulo.sizeDelta = dimensao;
            return objeto;
        }

        private static Text CriarTexto(Transform pai, string nome, string conteudo, Font fonte, int tamanho, TextAnchor alinhamento, Vector2 posicao, Vector2 dimensao)
        {
            GameObject objeto = CriarRetangulo(pai, nome, posicao, dimensao, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Text texto = objeto.AddComponent<Text>();
            texto.font = fonte;
            texto.text = conteudo;
            texto.fontSize = tamanho;
            texto.alignment = alinhamento;
            texto.color = Color.white;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.raycastTarget = false;
            return texto;
        }

        private static Button CriarBotao(Transform pai, string nome, string conteudo, Font fonte, Vector2 posicao)
        {
            GameObject objeto = CriarRetangulo(pai, nome, posicao, new Vector2(410f, 66f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Image imagem = objeto.AddComponent<Image>();
            imagem.color = new Color(0.11f, 0.13f, 0.19f, 0.98f);
            Outline contorno = objeto.AddComponent<Outline>();
            contorno.effectColor = new Color(0.76f, 0.58f, 0.34f, 0.9f);
            contorno.effectDistance = new Vector2(1.5f, -1.5f);
            Button botao = objeto.AddComponent<Button>();
            botao.targetGraphic = imagem;
            botao.transition = Selectable.Transition.ColorTint;
            botao.colors = new ColorBlock
            {
                normalColor = new Color(0.11f, 0.13f, 0.19f, 0.98f),
                highlightedColor = new Color(0.28f, 0.23f, 0.18f, 1f),
                pressedColor = new Color(0.58f, 0.39f, 0.2f, 1f),
                selectedColor = new Color(0.2f, 0.18f, 0.16f, 1f),
                disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f),
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };
            Text texto = CriarTexto(objeto.transform, "Texto", conteudo, fonte, 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(370f, 56f));
            texto.color = new Color(1f, 0.93f, 0.78f, 1f);
            return botao;
        }

        private static Slider CriarSlider(Transform pai, Font fonte, Vector2 posicao)
        {
            GameObject objeto = CriarRetangulo(pai, "ControleVolume", posicao, new Vector2(410f, 34f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            GameObject fundo = CriarRetangulo(objeto.transform, "Fundo", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            fundo.AddComponent<Image>().color = new Color(0.04f, 0.05f, 0.08f, 1f);
            GameObject preenchimentoArea = CriarRetangulo(objeto.transform, "AreaPreenchimento", new Vector2(-7f, 0f), new Vector2(-14f, -14f), Vector2.zero, Vector2.one, new Vector2(0f, 0.5f));
            GameObject preenchimento = CriarRetangulo(preenchimentoArea.transform, "Preenchimento", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f));
            Image imagemPreenchimento = preenchimento.AddComponent<Image>();
            imagemPreenchimento.color = new Color(0.76f, 0.53f, 0.28f, 1f);
            GameObject alcaArea = CriarRetangulo(objeto.transform, "AreaAlca", new Vector2(-7f, 0f), new Vector2(-14f, 0f), Vector2.zero, Vector2.one, new Vector2(0f, 0.5f));
            GameObject alca = CriarRetangulo(alcaArea.transform, "Alca", Vector2.zero, new Vector2(26f, 32f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f));
            Image imagemAlca = alca.AddComponent<Image>();
            imagemAlca.color = new Color(0.95f, 0.9f, 0.79f, 1f);
            Slider slider = objeto.AddComponent<Slider>();
            slider.fillRect = preenchimento.GetComponent<RectTransform>();
            slider.handleRect = alca.GetComponent<RectTransform>();
            slider.targetGraphic = imagemAlca;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;
            return slider;
        }

        private static Toggle CriarToggle(Transform pai, Font fonte, Vector2 posicao, string rotulo)
        {
            GameObject objeto = CriarRetangulo(pai, "ControleTelaCheia", posicao, new Vector2(410f, 52f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            GameObject caixa = CriarRetangulo(objeto.transform, "Caixa", new Vector2(-170f, 0f), new Vector2(30f, 30f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f));
            Image fundo = caixa.AddComponent<Image>();
            fundo.color = new Color(0.08f, 0.09f, 0.12f, 1f);
            Outline contorno = caixa.AddComponent<Outline>();
            contorno.effectColor = new Color(0.76f, 0.58f, 0.34f, 0.95f);
            contorno.effectDistance = new Vector2(2f, -2f);
            GameObject marca = CriarRetangulo(caixa.transform, "Marca", Vector2.zero, new Vector2(16f, 16f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Image imagemMarca = marca.AddComponent<Image>();
            imagemMarca.color = new Color(0.88f, 0.66f, 0.35f, 1f);
            Text texto = CriarTexto(objeto.transform, "Rotulo", rotulo, fonte, 20, TextAnchor.MiddleLeft, new Vector2(42f, 0f), new Vector2(300f, 44f));
            Toggle toggle = objeto.AddComponent<Toggle>();
            toggle.targetGraphic = fundo;
            toggle.graphic = imagemMarca;
            toggle.isOn = false;
            return toggle;
        }

        private static Font RecursosFonte()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void RemoverObjetoGerado(Scene cena, string nome)
        {
            foreach (GameObject objeto in cena.GetRootGameObjects())
            {
                if (objeto.name == nome)
                {
                    UnityEngine.Object.DestroyImmediate(objeto);
                }
            }
        }

        private static void PrepararArteDoMenu()
        {
            const string caminho = "Assets/Art/Menu/ImagemMenu.jpg";
            TextureImporter importador = AssetImporter.GetAtPath(caminho) as TextureImporter;
            if (importador == null)
            {
                throw new InvalidOperationException("Unity não encontrou a arte original do menu.");
            }

            if (importador.textureType == TextureImporterType.Sprite
                && importador.spriteImportMode == SpriteImportMode.Single
                && !importador.mipmapEnabled
                && importador.textureCompression == TextureImporterCompression.Uncompressed
                && importador.maxTextureSize >= 4096)
            {
                return;
            }

            importador.textureType = TextureImporterType.Sprite;
            importador.spriteImportMode = SpriteImportMode.Single;
            importador.mipmapEnabled = false;
            importador.textureCompression = TextureImporterCompression.Uncompressed;
            importador.maxTextureSize = 4096;
            importador.SaveAndReimport();
        }

        private static void Atribuir(SerializedObject objeto, string nome, UnityEngine.Object valor)
        {
            SerializedProperty propriedade = objeto.FindProperty(nome);
            if (propriedade == null)
            {
                throw new InvalidOperationException("Campo serializado ausente: " + nome);
            }

            propriedade.objectReferenceValue = valor;
        }
    }
}
