# Avalonya_Unity

Versão Unity do jogo 2.5D **Avalonya**.

- **Unity:** 6000.6.4f1
- **Renderização:** Universal Render Pipeline (URP)
- **Cena de entrada:** `Assets/Scenes/MenuAvalonya.unity`
- **Relatório da migração:** [`Docs/MIGRACAO.md`](Docs/MIGRACAO.md)

## Abrir o projeto

1. Clone este repositório pelo GitHub Desktop.
2. No Unity Hub, escolha **Add** e selecione a pasta clonada.
3. Abra com Unity 6000.6.4f1.

## Arquivos do projeto

`Assets`, `Packages` e `ProjectSettings` contêm o projeto Unity compartilhado. Caches locais do editor, arquivos temporários e soluções geradas ficam fora do Git.

## Colaboração

Crie uma branch para cada conjunto de mudanças e abra um Pull Request para revisão antes de integrar na branch principal. Mantenha juntos cada arquivo `.meta` e seu asset correspondente. O Smart Merge do Unity está configurado para cenas, prefabs e assets serializados; cada colaborador precisa apontá-lo à instalação local do Unity.

No macOS com esta versão do Unity, o comando abaixo configura o Smart Merge para o repositório local:

```sh
git config merge.unityyamlmerge.name "Unity Smart Merge"
git config merge.unityyamlmerge.driver "'/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Helpers/UnityYAMLMerge' merge -p %O %B %A %A"
```

Se o Unity estiver instalado em outro local ou sistema operacional, ajuste o caminho do executável.
