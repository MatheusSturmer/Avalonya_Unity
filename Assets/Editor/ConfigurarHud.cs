using Avalonya;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Avalonya.Editor
{
    public static class ConfigurarHud
    {
        private const string CaminhoCena = "Assets/Scenes/AvalonyaWorld.unity";

        [MenuItem("Avalonya/Migracao/Configurar HUD de vida e progresso")]
        public static void Configurar()
        {
            Scene cena = EditorSceneManager.OpenScene(CaminhoCena, OpenSceneMode.Single);
            GameObject hudAntigo = GameObject.Find("HUD Avalonya");
            if (hudAntigo != null)
            {
                Object.DestroyImmediate(hudAntigo);
            }

            GameObject canvasObjeto = new GameObject(
                "HUD Avalonya",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)
            );
            SceneManager.MoveGameObjectToScene(canvasObjeto, cena);
            canvasObjeto.transform.localScale = Vector3.one;
            Canvas canvas = canvasObjeto.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler escalador = canvasObjeto.GetComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);
            escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            escalador.matchWidthOrHeight = 0.5f;

            Font fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (fonte == null)
            {
                fonte = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            Image vida;
            Image xp;
            Text textoVida;
            Text textoDano;
            Text textoXp;
            Text textoTempo;
            Text textoAbates;
            Text textoInimigos;

            CriarPainel(canvasObjeto.transform, "PainelVida", new Vector2(24f, -24f), new Vector2(340f, 112f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            Transform painelVida = GameObject.Find("PainelVida").transform;
            CriarTexto(painelVida, "TituloVida", "VIDA", fonte, 17, TextAnchor.MiddleLeft, new Vector2(18f, -10f), new Vector2(304f, 24f), new Vector2(0f, 1f));
            Image fundoVida = CriarBarra(painelVida, "BarraVida", new Vector2(18f, -43f), new Vector2(304f, 25f), new Color(0.13f, 0.07f, 0.09f, 1f), new Color(0.83f, 0.25f, 0.31f, 1f), new Vector2(0f, 1f), out vida);
            textoVida = CriarTexto(fundoVida.transform, "ValorVida", "12 / 12", fonte, 15, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero, Vector2.one);
            EsticarTexto(textoVida);
            textoDano = CriarTexto(painelVida, "TextoDano", "DANO  2", fonte, 15, TextAnchor.MiddleLeft, new Vector2(18f, -76f), new Vector2(304f, 22f), new Vector2(0f, 1f));

            CriarPainel(canvasObjeto.transform, "PainelXp", new Vector2(24f, 24f), new Vector2(420f, 72f), Vector2.zero, Vector2.zero);
            Transform painelXp = GameObject.Find("PainelXp").transform;
            CriarTexto(painelXp, "TituloXp", "EXPERIÊNCIA", fonte, 15, TextAnchor.MiddleLeft, new Vector2(16f, -10f), new Vector2(388f, 20f), new Vector2(0f, 1f));
            Image fundoXp = CriarBarra(painelXp, "BarraXp", new Vector2(16f, -38f), new Vector2(388f, 20f), new Color(0.05f, 0.12f, 0.13f, 1f), new Color(0.21f, 0.8f, 0.65f, 1f), new Vector2(0f, 1f), out xp);
            textoXp = CriarTexto(fundoXp.transform, "ValorXp", "0 / 10", fonte, 13, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero, Vector2.one);
            EsticarTexto(textoXp);

            CriarPainel(canvasObjeto.transform, "PainelPartida", new Vector2(-24f, -24f), new Vector2(300f, 134f), Vector2.one, Vector2.one);
            Transform painelPartida = GameObject.Find("PainelPartida").transform;
            textoTempo = CriarTexto(painelPartida, "TempoEtapa", "00:00  |  ETAPA 1", fonte, 22, TextAnchor.MiddleRight, new Vector2(-20f, -12f), new Vector2(260f, 36f), Vector2.one);
            textoInimigos = CriarTexto(painelPartida, "StatusInimigos", "SLIMES  0/15  |  ARANHAS  0/5", fonte, 15, TextAnchor.MiddleRight, new Vector2(-20f, -61f), new Vector2(260f, 22f), Vector2.one);
            textoAbates = CriarTexto(painelPartida, "StatusPartida", "ABATES  0  |  ESC: PAUSA", fonte, 15, TextAnchor.MiddleRight, new Vector2(-20f, -92f), new Vector2(260f, 22f), Vector2.one);

            Hud hud = canvasObjeto.AddComponent<Hud>();
            SerializedObject serializado = new SerializedObject(hud);
            serializado.FindProperty("preenchimentoVida").objectReferenceValue = vida;
            serializado.FindProperty("preenchimentoXp").objectReferenceValue = xp;
            serializado.FindProperty("textoVida").objectReferenceValue = textoVida;
            serializado.FindProperty("textoDano").objectReferenceValue = textoDano;
            serializado.FindProperty("textoXp").objectReferenceValue = textoXp;
            serializado.FindProperty("textoTempo").objectReferenceValue = textoTempo;
            serializado.FindProperty("textoAbates").objectReferenceValue = textoAbates;
            serializado.FindProperty("textoInimigos").objectReferenceValue = textoInimigos;
            serializado.ApplyModifiedPropertiesWithoutUndo();

            var escalaCanvas = new SerializedObject(canvasObjeto.transform);
            escalaCanvas.FindProperty("m_LocalScale").vector3Value = Vector3.one;
            escalaCanvas.ApplyModifiedPropertiesWithoutUndo();
            canvasObjeto.transform.localScale = Vector3.one;
            EditorSceneManager.MarkSceneDirty(cena);
            if (!EditorSceneManager.SaveScene(cena))
            {
                throw new System.IO.IOException("Unity não conseguiu salvar o HUD na cena.");
            }

            Scene cenaReaberta = EditorSceneManager.OpenScene(CaminhoCena, OpenSceneMode.Single);
            GameObject hudSalvo = GameObject.Find("HUD Avalonya");
            if (cenaReaberta.path != CaminhoCena || hudSalvo == null || hudSalvo.transform.localScale != Vector3.one)
            {
                throw new System.InvalidOperationException("A cena reaberta não preservou a escala visível do HUD.");
            }

            Debug.Log("HUD configurado: vida, dano, XP, tempo, etapa, slimes, aranhas, abates e pausa.");
        }

        private static GameObject CriarPainel(Transform pai, string nome, Vector2 posicao, Vector2 tamanho, Vector2 ancoraMinima, Vector2 ancoraMaxima)
        {
            GameObject objeto = CriarRetangulo(pai, nome, posicao, tamanho, ancoraMinima, ancoraMaxima, ancoraMinima);
            Image imagem = objeto.AddComponent<Image>();
            imagem.color = new Color(0.025f, 0.035f, 0.065f, 0.88f);
            return objeto;
        }

        private static Image CriarBarra(Transform pai, string nome, Vector2 posicao, Vector2 tamanho, Color fundo, Color corPreenchimento, Vector2 ancora, out Image preenchimento)
        {
            const string caminhoSpriteBarra = "Assets/Art/Generated/UI/PixelBranco.png";
            TextureImporter importador = AssetImporter.GetAtPath(caminhoSpriteBarra) as TextureImporter;
            if (importador == null)
            {
                AssetDatabase.ImportAsset(caminhoSpriteBarra, ImportAssetOptions.ForceUpdate);
                importador = AssetImporter.GetAtPath(caminhoSpriteBarra) as TextureImporter;
            }

            if (importador == null)
            {
                throw new System.InvalidOperationException("Unity não conseguiu importar o sprite branco usado pelas barras.");
            }

            bool precisaReimportar = importador.textureType != TextureImporterType.Sprite
                || importador.spriteImportMode != SpriteImportMode.Single
                || !importador.alphaIsTransparency
                || importador.mipmapEnabled;
            if (precisaReimportar)
            {
                importador.textureType = TextureImporterType.Sprite;
                importador.spriteImportMode = SpriteImportMode.Single;
                importador.alphaIsTransparency = true;
                importador.mipmapEnabled = false;
                importador.SaveAndReimport();
            }

            Sprite spriteBarra = AssetDatabase.LoadAssetAtPath<Sprite>(caminhoSpriteBarra);
            if (spriteBarra == null)
            {
                throw new System.InvalidOperationException("Unity não conseguiu importar o sprite branco usado pelas barras.");
            }

            GameObject fundoObjeto = CriarRetangulo(pai, nome, posicao, tamanho, ancora, ancora, new Vector2(0f, 1f));
            Image imagemFundo = fundoObjeto.AddComponent<Image>();
            imagemFundo.color = fundo;

            GameObject preenchimentoObjeto = CriarRetangulo(fundoObjeto.transform, "Preenchimento", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f));
            RectTransform retangulo = preenchimentoObjeto.GetComponent<RectTransform>();
            retangulo.offsetMin = new Vector2(3f, 3f);
            retangulo.offsetMax = new Vector2(-3f, -3f);
            preenchimento = preenchimentoObjeto.AddComponent<Image>();
            preenchimento.sprite = spriteBarra;
            preenchimento.color = corPreenchimento;
            preenchimento.type = Image.Type.Filled;
            preenchimento.fillMethod = Image.FillMethod.Horizontal;
            preenchimento.fillOrigin = (int)Image.OriginHorizontal.Left;
            preenchimento.fillAmount = 1f;
            return imagemFundo;
        }

        private static Text CriarTexto(Transform pai, string nome, string conteudo, Font fonte, int tamanho, TextAnchor alinhamento, Vector2 posicao, Vector2 dimensao, Vector2 ancora)
        {
            GameObject objeto = CriarRetangulo(pai, nome, posicao, dimensao, ancora, ancora, ancora);
            Text texto = objeto.AddComponent<Text>();
            texto.text = conteudo;
            texto.font = fonte;
            texto.fontSize = tamanho;
            texto.fontStyle = FontStyle.Bold;
            texto.alignment = alinhamento;
            texto.color = new Color(0.94f, 0.95f, 0.97f, 1f);
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.verticalOverflow = VerticalWrapMode.Truncate;
            texto.raycastTarget = false;
            return texto;
        }

        private static void EsticarTexto(Text texto)
        {
            RectTransform retangulo = texto.rectTransform;
            retangulo.anchorMin = Vector2.zero;
            retangulo.anchorMax = Vector2.one;
            retangulo.pivot = new Vector2(0.5f, 0.5f);
            retangulo.offsetMin = Vector2.zero;
            retangulo.offsetMax = Vector2.zero;
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
