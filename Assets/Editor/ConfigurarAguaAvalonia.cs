using System;
using System.IO;
using Avalonia;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Avalonia.Editor
{
    public static class ConfigurarAguaAvalonia
    {
        private const string CaminhoCena = "Assets/Scenes/AvalonyaWorld.unity";
        private const string CaminhoExportacao = "Assets/Editor/Data/cenario-godot.json";
        private const string NomeRaiz = "VolumesAguaAvalonia";

        [MenuItem("Avalonia/Migracao/Configurar volumes de agua")]
        public static void ConfigurarVolumes()
        {
            string caminhoCompleto = Path.GetFullPath(CaminhoExportacao);
            if (!File.Exists(caminhoCompleto))
            {
                throw new FileNotFoundException("A exportação do cenário Godot não foi encontrada.", caminhoCompleto);
            }

            ExportacaoCenario exportacao = JsonUtility.FromJson<ExportacaoCenario>(File.ReadAllText(caminhoCompleto));
            if (exportacao == null || exportacao.colliders == null)
            {
                throw new InvalidDataException("A exportação do cenário não contém a lista de colisores.");
            }

            Scene cena = EditorSceneManager.OpenScene(CaminhoCena, OpenSceneMode.Single);
            GameObject raiz = GameObject.Find(NomeRaiz);
            if (raiz == null)
            {
                raiz = new GameObject(NomeRaiz);
            }

            raiz.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            raiz.transform.localScale = Vector3.one;

            int configurados = 0;
            foreach (ColisorExportado origem in exportacao.colliders)
            {
                if (origem == null || origem.shape != "BoxShape3D"
                    || string.IsNullOrEmpty(origem.path)
                    || !origem.path.StartsWith("MundoTeste/Ambiente/Agua", StringComparison.Ordinal))
                {
                    continue;
                }

                ConfigurarVolume(raiz.transform, origem);
                configurados++;
            }

            if (configurados != 12)
            {
                throw new InvalidDataException($"Esperava 12 volumes de água do Godot, mas encontrei {configurados}.");
            }

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena, CaminhoCena);
            AssetDatabase.SaveAssets();
            Debug.Log($"Etapa água configurada: {configurados} volumes, com velocidade 2 e FOV 50.");
        }

        private static void ConfigurarVolume(Transform raiz, ColisorExportado origem)
        {
            string nome = origem.path.Substring("MundoTeste/Ambiente/".Length).Split('/')[0];
            Transform volumeExistente = raiz.Find(nome);
            GameObject volume = volumeExistente != null ? volumeExistente.gameObject : new GameObject(nome);
            volume.transform.SetParent(raiz, false);

            Vector3 posicao = Vetor(origem.position);
            Vector3 direita = Vetor(origem.right);
            Vector3 cima = Vetor(origem.up);
            Vector3 frente = Vetor(origem.forward);
            Vector3 tamanho = Vetor(origem.size);

            volume.transform.SetPositionAndRotation(
                posicao,
                Quaternion.LookRotation(frente.normalized, cima.normalized)
            );
            volume.transform.localScale = new Vector3(direita.magnitude, cima.magnitude, frente.magnitude);

            BoxCollider colisor = volume.GetComponent<BoxCollider>();
            if (colisor == null)
            {
                colisor = volume.AddComponent<BoxCollider>();
            }

            colisor.size = tamanho;
            colisor.center = Vector3.zero;
            colisor.isTrigger = true;

            Rigidbody corpo = volume.GetComponent<Rigidbody>();
            if (corpo == null)
            {
                corpo = volume.AddComponent<Rigidbody>();
            }

            corpo.isKinematic = true;
            corpo.useGravity = false;

            AreaAguaAvalonia agua = volume.GetComponent<AreaAguaAvalonia>();
            if (agua == null)
            {
                agua = volume.AddComponent<AreaAguaAvalonia>();
            }

            SerializedObject serializado = new SerializedObject(agua);
            serializado.FindProperty("velocidadeAgua").floatValue = 2f;
            serializado.FindProperty("campoVisaoAgua").floatValue = 50f;
            serializado.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Vector3 Vetor(float[] valores)
        {
            if (valores == null || valores.Length < 3)
            {
                throw new InvalidDataException("Um vetor do cenário não tem três componentes.");
            }

            return new Vector3(valores[0], valores[1], valores[2]);
        }

        [Serializable]
        private sealed class ExportacaoCenario
        {
            public ColisorExportado[] colliders;
        }

        [Serializable]
        private sealed class ColisorExportado
        {
            public string path;
            public string shape;
            public float[] position;
            public float[] right;
            public float[] up;
            public float[] forward;
            public float[] size;
        }
    }
}
