using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Avalonya.Editor
{
    public static class ConfigurarCartas
    {
        private const string CaminhoCena = "Assets/Scenes/AvalonyaWorld.unity";
        private const string PastaProgressao = "Assets/Art/Progressao";

        [MenuItem("Avalonya/Migracao/Configurar cartas e machado automatico")]
        public static void Configurar()
        {
            Scene cena = EditorSceneManager.OpenScene(CaminhoCena, OpenSceneMode.Single);
            ConfigurarCartasDoEstado();
            ConfigurarMachado();
            ConfigurarSistemaDeEventos(cena);
            GameObject interfaceAntiga = GameObject.Find("Interface de Cartas Avalonya");
            if (interfaceAntiga != null)
            {
                UnityEngine.Object.DestroyImmediate(interfaceAntiga);
            }

            Font fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            GameObject canvasObjeto = new GameObject(
                "Interface de Cartas Avalonya",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(InterfaceCartas)
            );
            SceneManager.MoveGameObjectToScene(canvasObjeto, cena);
            Canvas canvas = canvasObjeto.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler escalador = canvasObjeto.GetComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);
            escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            escalador.matchWidthOrHeight = 0.5f;

            GameObject painel = CriarRetangulo(canvasObjeto.transform, "PainelCartas", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            Image fundo = painel.AddComponent<Image>();
            fundo.color = new Color(0.025f, 0.035f, 0.065f, 0.96f);
            Image imagemFundo = fundo;

            Text titulo = CriarTexto(painel.transform, "TituloCartas", "ESCOLHA UMA CARTA", fonte, 34, TextAnchor.MiddleCenter, new Vector2(0f, 355f), new Vector2(920f, 54f));
            titulo.color = new Color(1f, 0.89f, 0.62f, 1f);
            Text instrucao = CriarTexto(painel.transform, "InstrucaoCartas", "Escolha uma melhoria para continuar", fonte, 19, TextAnchor.MiddleCenter, new Vector2(0f, 307f), new Vector2(920f, 34f));

            VisualCarta[] visuais = new VisualCarta[3];
            Sprite[] sprites = new Sprite[10];
            string[] caminhos = {
                "forca.png", "vitalidade.png", "cura.png", "acelerar.png", "destreza.png",
                "machado.png", "machado_animacao/machado_1.png", "machado_animacao/machado_2.png",
                "machado_animacao/machado_3.png", "machado_quantidade.png"
            };
            for (int indice = 0; indice < caminhos.Length; indice++)
            {
                int pixelsPorUnidade = caminhos[indice].StartsWith("machado_animacao/", StringComparison.Ordinal) ? 350 : 100;
                sprites[indice] = ImportarSprite(PastaProgressao + "/" + caminhos[indice], pixelsPorUnidade);
            }

            float[] posicoesX = { -332f, 0f, 332f };
            for (int indice = 0; indice < visuais.Length; indice++)
            {
                GameObject cartao = CriarRetangulo(painel.transform, "CartaVisual_" + (indice + 1), new Vector2(posicoesX[indice], -24f), new Vector2(292f, 438f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                Image fundoCarta = cartao.AddComponent<Image>();
                fundoCarta.color = new Color(0.10f, 0.15f, 0.23f, 1f);
                Outline contorno = cartao.AddComponent<Outline>();
                contorno.effectColor = new Color(0.78f, 0.56f, 0.27f, 0.95f);
                contorno.effectDistance = new Vector2(2f, -2f);

                GameObject imagemObjeto = CriarRetangulo(cartao.transform, "ImagemCarta", new Vector2(0f, 122f), new Vector2(186f, 186f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                Image imagem = imagemObjeto.AddComponent<Image>();
                imagem.sprite = sprites[indice];
                imagem.preserveAspect = true;
                imagem.raycastTarget = false;

                Text tituloCarta = CriarTexto(cartao.transform, "TituloCarta", "", fonte, 22, TextAnchor.MiddleCenter, new Vector2(0f, 5f), new Vector2(260f, 48f));
                tituloCarta.color = new Color(1f, 0.89f, 0.62f, 1f);
                Text descricaoCarta = CriarTexto(cartao.transform, "DescricaoCarta", "", fonte, 17, TextAnchor.MiddleCenter, new Vector2(0f, -68f), new Vector2(260f, 88f));
                descricaoCarta.horizontalOverflow = HorizontalWrapMode.Wrap;

                GameObject botaoObjeto = CriarRetangulo(cartao.transform, "BotaoSelecionar", new Vector2(0f, -174f), new Vector2(236f, 52f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                Image imagemBotao = botaoObjeto.AddComponent<Image>();
                imagemBotao.color = new Color(0.83f, 0.58f, 0.25f, 1f);
                Button botao = botaoObjeto.AddComponent<Button>();
                botao.targetGraphic = imagemBotao;
                botao.transition = Selectable.Transition.ColorTint;
                Text textoBotao = CriarTexto(botaoObjeto.transform, "TextoBotao", "SELECIONAR", fonte, 17, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(224f, 42f));
                textoBotao.color = new Color(0.08f, 0.10f, 0.14f, 1f);

                VisualCarta visual = cartao.AddComponent<VisualCarta>();
                SerializedObject visualSerializado = new SerializedObject(visual);
                visualSerializado.FindProperty("imagemCarta").objectReferenceValue = imagem;
                visualSerializado.FindProperty("textoTitulo").objectReferenceValue = tituloCarta;
                visualSerializado.FindProperty("textoDescricao").objectReferenceValue = descricaoCarta;
                visualSerializado.FindProperty("botao").objectReferenceValue = botao;
                visualSerializado.ApplyModifiedPropertiesWithoutUndo();
                visuais[indice] = visual;
            }

            SerializedObject interfaceSerializada = new SerializedObject(canvasObjeto.GetComponent<InterfaceCartas>());
            interfaceSerializada.FindProperty("painelCartas").objectReferenceValue = painel;
            interfaceSerializada.FindProperty("tituloPainel").objectReferenceValue = titulo;
            interfaceSerializada.FindProperty("instrucaoPainel").objectReferenceValue = instrucao;
            SerializedProperty propriedadeVisuais = interfaceSerializada.FindProperty("visuaisCartas");
            propriedadeVisuais.arraySize = visuais.Length;
            for (int indice = 0; indice < visuais.Length; indice++)
            {
                propriedadeVisuais.GetArrayElementAtIndex(indice).objectReferenceValue = visuais[indice];
            }
            interfaceSerializada.ApplyModifiedPropertiesWithoutUndo();
            painel.SetActive(false);

            SerializedObject escalaCanvas = new SerializedObject(canvasObjeto.transform);
            escalaCanvas.FindProperty("m_LocalScale").vector3Value = Vector3.one;
            escalaCanvas.ApplyModifiedPropertiesWithoutUndo();
            canvasObjeto.transform.localScale = Vector3.one;
            EditorSceneManager.MarkSceneDirty(cena);
            if (!EditorSceneManager.SaveScene(cena))
            {
                throw new System.IO.IOException("Unity não conseguiu salvar a interface de cartas.");
            }

            Scene cenaReaberta = EditorSceneManager.OpenScene(CaminhoCena, OpenSceneMode.Single);
            InterfaceCartas interfaceSalva = UnityEngine.Object.FindAnyObjectByType<InterfaceCartas>();
            EventSystem eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            MachadoAutomatico machadoSalvo = UnityEngine.Object.FindAnyObjectByType<MachadoAutomatico>();
            EstadoJogo estadoSalvo = UnityEngine.Object.FindAnyObjectByType<EstadoJogo>();
            if (cenaReaberta.path != CaminhoCena || interfaceSalva == null || eventSystem == null
                || eventSystem.GetComponent<InputSystemUIInputModule>() == null || machadoSalvo == null
                || estadoSalvo == null || interfaceSalva.transform.localScale != Vector3.one)
            {
                throw new InvalidOperationException("A cena reaberta não preservou as cartas, o EventSystem ou o machado automático.");
            }

            Debug.Log("Progressão configurada: 10 cartas, escolha pausada, melhorias e machado automático.");
        }

        private static void ConfigurarCartasDoEstado()
        {
            EstadoJogo estado = UnityEngine.Object.FindAnyObjectByType<EstadoJogo>();
            if (estado == null)
            {
                throw new InvalidOperationException("A cena precisa de EstadoJogo antes de configurar as cartas.");
            }

            string[] ids = { "forca", "vitalidade", "cura", "acelerar", "destreza", "machado", "machado_dano", "machado_velocidade", "machado_intervalo", "machado_quantidade" };
            string[] titulos = { "FORÇA", "VITALIDADE", "RECUPERAÇÃO", "ACELERAR", "DESTREZA", "MACHADO", "LÂMINA FORTE", "LÂMINA VELOZ", "ARREMESSO RÁPIDO", "MACHADOS DUPLOS" };
            string[] descricoes = {
                "+1 de dano\nno seu ataque corpo a corpo.",
                "+2 de vida máxima\ne recupera 2 de vida.",
                "Recupera toda\na sua vida atual.",
                "Aumenta a velocidade\nde movimento em 10%.",
                "Aumenta a velocidade\nde ataque em 20%.",
                "Libera o machado:\nele voa sozinho no inimigo mais próximo.",
                "+1 de dano\nsó no machado.",
                "Machado voa 20%\nmais rápido.",
                "Machado sai 2 s\nmais cedo (mín. 2 s).",
                "Lança mais um machado\nem outro inimigo."
            };
            string[] caminhos = {
                "forca.png", "vitalidade.png", "cura.png", "acelerar.png", "destreza.png",
                "machado.png", "machado_animacao/machado_1.png", "machado_animacao/machado_2.png",
                "machado_animacao/machado_3.png", "machado_quantidade.png"
            };

            SerializedObject serializado = new SerializedObject(estado);
            SerializedProperty cartas = serializado.FindProperty("cartas");
            cartas.arraySize = ids.Length;
            for (int indice = 0; indice < ids.Length; indice++)
            {
                SerializedProperty item = cartas.GetArrayElementAtIndex(indice);
                item.FindPropertyRelative("id").stringValue = ids[indice];
                item.FindPropertyRelative("titulo").stringValue = titulos[indice];
                item.FindPropertyRelative("descricao").stringValue = descricoes[indice];
                item.FindPropertyRelative("imagem").objectReferenceValue = ImportarSprite(
                    PastaProgressao + "/" + caminhos[indice],
                    caminhos[indice].StartsWith("machado_animacao/", StringComparison.Ordinal) ? 350 : 100
                );
            }
            serializado.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurarMachado()
        {
            Jogador jogador = UnityEngine.Object.FindAnyObjectByType<Jogador>();
            if (jogador == null)
            {
                throw new InvalidOperationException("A cena precisa de Jogador para configurar o machado.");
            }

            MachadoAutomatico machado = jogador.GetComponent<MachadoAutomatico>();
            if (machado == null)
            {
                machado = jogador.gameObject.AddComponent<MachadoAutomatico>();
            }

            Sprite[] quadros = {
                ImportarSprite(PastaProgressao + "/machado_animacao/machado_1.png", 350),
                ImportarSprite(PastaProgressao + "/machado_animacao/machado_2.png", 350),
                ImportarSprite(PastaProgressao + "/machado_animacao/machado_3.png", 350),
                ImportarSprite(PastaProgressao + "/machado_animacao/machado_4.png", 350)
            };
            SerializedObject serializado = new SerializedObject(machado);
            SerializedProperty quadrosSalvos = serializado.FindProperty("quadrosMachado");
            quadrosSalvos.arraySize = quadros.Length;
            for (int indice = 0; indice < quadros.Length; indice++)
            {
                quadrosSalvos.GetArrayElementAtIndex(indice).objectReferenceValue = quadros[indice];
            }
            serializado.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurarSistemaDeEventos(Scene cena)
        {
            EventSystem sistema = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (sistema == null)
            {
                GameObject objeto = new GameObject("EventSystem Avalonya", typeof(EventSystem), typeof(InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(objeto, cena);
                sistema = objeto.GetComponent<EventSystem>();
            }

            if (sistema.GetComponent<InputSystemUIInputModule>() == null)
            {
                UnityEngine.Object.DestroyImmediate(sistema.GetComponent<BaseInputModule>());
                sistema.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private static Sprite ImportarSprite(string caminho, int pixelsPorUnidade)
        {
            AssetDatabase.ImportAsset(caminho, ImportAssetOptions.ForceUpdate);
            TextureImporter importador = AssetImporter.GetAtPath(caminho) as TextureImporter;
            if (importador == null)
            {
                throw new InvalidOperationException("Unity não encontrou a imagem de carta: " + caminho);
            }

            FilterMode filtro = caminho.EndsWith("/acelerar.png", StringComparison.Ordinal)
                || caminho.EndsWith("/destreza.png", StringComparison.Ordinal)
                ? FilterMode.Point
                : FilterMode.Bilinear;
            bool precisaReimportar = importador.textureType != TextureImporterType.Sprite
                || importador.spriteImportMode != SpriteImportMode.Single
                || !importador.alphaIsTransparency
                || importador.mipmapEnabled
                || importador.filterMode != filtro
                || importador.textureCompression != TextureImporterCompression.Uncompressed
                || !Mathf.Approximately(importador.spritePixelsPerUnit, pixelsPorUnidade);
            if (precisaReimportar)
            {
                importador.textureType = TextureImporterType.Sprite;
                importador.spriteImportMode = SpriteImportMode.Single;
                importador.alphaIsTransparency = true;
                importador.mipmapEnabled = false;
                importador.filterMode = filtro;
                importador.textureCompression = TextureImporterCompression.Uncompressed;
                importador.spritePixelsPerUnit = pixelsPorUnidade;
                importador.SaveAndReimport();
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
            if (sprite == null)
            {
                throw new InvalidOperationException("Unity não conseguiu carregar a imagem da carta: " + caminho);
            }
            return sprite;
        }

        private static Text CriarTexto(Transform pai, string nome, string conteudo, Font fonte, int tamanho, TextAnchor alinhamento, Vector2 posicao, Vector2 dimensao)
        {
            GameObject objeto = CriarRetangulo(pai, nome, posicao, dimensao, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Text texto = objeto.AddComponent<Text>();
            texto.text = conteudo;
            texto.font = fonte;
            texto.fontSize = tamanho;
            texto.fontStyle = FontStyle.Bold;
            texto.alignment = alinhamento;
            texto.color = new Color(0.94f, 0.95f, 0.97f, 1f);
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Truncate;
            texto.raycastTarget = false;
            return texto;
        }

        private static GameObject CriarRetangulo(Transform pai, string nome, Vector2 posicao, Vector2 tamanho, Vector2 ancoraMinima, Vector2 ancoraMaxima, Vector2 pivo)
        {
            GameObject objeto = new GameObject(nome, typeof(RectTransform));
            objeto.transform.SetParent(pai, false);
            RectTransform retangulo = objeto.GetComponent<RectTransform>();
            retangulo.anchorMin = ancoraMinima;
            retangulo.anchorMax = ancoraMaxima;
            retangulo.pivot = pivo;
            retangulo.anchoredPosition = posicao;
            retangulo.sizeDelta = tamanho;
            return objeto;
        }
    }
}
