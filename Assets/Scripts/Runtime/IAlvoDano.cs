using UnityEngine;

namespace Avalonya
{
    /// <summary>Contrato simples para inimigos que podem receber ataques do jogador.</summary>
    public interface IAlvoDano
    {
        bool EstaMorto { get; }
        Vector3 PontoDano { get; }
        float RaioHorizontal { get; }
        void ReceberDano(float quantidade, Vector3 origem, GameObject autor);
    }

    /// <summary>Permite que o machado altere o recuo de um inimigo que sobreviveu ao golpe.</summary>
    public interface IRecuoMachado
    {
        void RedirecionarRecuoMachado(Vector3 impulso);
    }

    /// <summary>Recebe e desfaz o efeito compartilhado dos volumes de água.</summary>
    public interface ICorpoAgua
    {
        void EntrarNaAgua(float velocidadeAgua, float campoVisaoAgua);
        void SairDaAgua();
    }
}
