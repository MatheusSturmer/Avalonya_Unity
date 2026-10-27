# Migração do Avalonya para Unity

O projeto Unity se chama **Avalonia**. O jogo continua com o título **Avalonya**.

## Referência e decisões

- Projeto Godot de referência: `/Users/matheus/Documents/GitHub/GameJamPlus_2026`.
- Cena de jogo: `Scenes/mundo_teste.tscn`. A cena de inicialização do Godot é `Scenes/Menu/CRTShaderMenuTest.tscn`.
- A arte `Scenes/BackGroundGround.png` está presente no clone de referência como alteração local sem commit; foi copiada sem alterar a origem.
- A cena Unity usa URP 3D, mundo no plano X/Z e câmera em perspectiva, para manter o enquadramento 2.5D com sprites voltados à câmera.

## Etapas

1. **Projeto, arte e cenário — concluída:** projeto Unity 6000.6.4f1, cena 2.5D com mapa, chão físico, iluminação, cenário importado e pontos de entrada. Importação das imagens usadas na cena conferida.
2. **Personagem e câmera — concluída para este checkpoint:** movimento X/Z, gravidade, animações direcionais, preparação de dano/recuo, derrapagem e acompanhamento com mola. Personagem, câmera e cenário foram verificados no Play Mode e revisados visualmente.
3. **Inimigos e combate — migrado para a cena ativa (3A–3E):** Slime e aranha comum, enxame, perseguição, ondas, ataques, água, dano, morte, XP e recuo.
4. **Progressão e HUD — migrado para a cena ativa (4A–4C):** coleta de XP, abates, cartas, melhorias, machado automático e HUD. O HUD informa vida, dano, XP, tempo, etapa, slimes/aranhas, abates e pausa por Esc.
5. **Menu e fluxo — migrados e validados:** menu, opções, créditos, áudio, pausa, fim de partida, reinício/retorno ao menu e cenas de build.
6. **Cobertura e diferenças conhecidas:** a auditoria independente não encontrou sistemas centrais da cena ativa sem contraparte. Persiste uma pequena aproximação de colisão descrita abaixo.

O escopo é a cena inicial do menu, a cena ativa `mundo_teste.tscn` e os dois autoloads ligados a esse fluxo. Cenas de protótipo que não são referenciadas por esse percurso não entram na build do Unity.

## Validação incremental

Em cada etapa, registrar separadamente importação, compilação, execução e revisão visual. Uma validação estrutural do projeto não confirma a execução, e uma validação de execução não substitui a inspeção visual.

### Checkpoint 3A — Slime sem combate

- **Importação:** walk_1, walk_2, attack_1 e attack_2 configurados como sprites pontilhados com transparência; o prefab usa somente os dois quadros de caminhada nesta etapa.
- **Compilação:** scripts runtime e configurador de cena compilam no Unity 6000.6.4f1 sem erros ou avisos.
- **Execução:** teste no Play Mode confirmou referências de jogador/prefab, animação alternando entre os dois quadros, slimes aterrados e em perseguição. No estágio de 30 segundos, a contagem chegou ao limite 30; dos 30 slimes, 15 estavam se movendo e os oito dentro de 1,45 m tinham velocidade horizontal zero. Não houve erros nem avisos no Console.
- **Revisão visual:** captura do Play Mode inspecionada com mapa, personagem e slimes visíveis. Em partidas prolongadas, os inimigos se acumulam ao redor do jogador porque ainda não podem morrer; a próxima etapa precisa integrar vida, ataques e abates. Esta revisão cobre o checkpoint 3A, não a migração completa.
- **Diferença conhecida:** o Unity usa CharacterController cápsula (altura 1,15 m e raio 0,4 m) para movimentar o slime, enquanto o Godot usa caixa 1,05 × 1,15 × 0,8 m. Isso mantém o controlador compatível com a cena Unity, mas aproxima a largura de colisão; revisar ao portar combate e colisões de ataque.

### Checkpoint 3B — Vida e cabeçada

- **Importação/configuração:** os dois quadros `attack_1`/`attack_2` foram preparados como sprites e ligados ao prefab. O jogador e os slimes estão na camada de personagens; a checagem de obstáculos usa a camada Default, equivalente à máscara ambiental 1 do Godot.
- **Compilação:** 0 erros e 0 avisos no Unity 6000.6.4f1. Uma primeira execução do configurador parou antes de salvar por falta do campo serializado `vidaMaxima`; o campo foi adicionado e o configurador passou a validar nomes de propriedades antes de preenchê-las. A repetição salvou prefab e cena.
- **Execução:** o jogador inicia com 12 de vida. A cabeçada mantém alcance de início 1,45 m, alcance de acerto 1,75 m, dano 2, duração 1,2 s, recarga 1,3 s e investida de 1,5 m/s; a animação usa os dois quadros originais. Um teste estendido registrou seis acertos até 0/12 de vida. Depois da morte, por mais 2 s, os totais ficaram em 14 slimes, 7 ataques iniciados e 6 acertos; nenhum slime continuou atacando e o spawner não criou novos. Console: 0 erros e 0 avisos.
- **Revisão visual:** captura do Play Mode inspecionada com o mapa, o personagem e as animações dos slimes. A captura foi feita numa aglomeração após o jogador morrer; não é uma imagem de apresentação. A tela de fim de jogo ainda não existe.
- **Pendências ao fechar 3B:** morrer bloqueava o controle do jogador, paralisava os slimes existentes e interrompia novos surgimentos. Pausa global e interface de fim de jogo ficaram para a etapa de fluxo/HUD.

### Checkpoint 3C — Ataque manual e morte do slime

- **Importação/configuração:** `ataque_basico.png` e `slime_knockback.png` foram importados como sprites pontilhados, sem compressão e com transparência. O jogador tem o ataque no clique esquerdo; o prefab de slime serializa 4 de vida, força de recuo 8, duração 0,22 s e raio de dano 0,66 m. Esse raio reproduz o alcance radial dos cantos da caixa Godot de 1,05 × 0,8 m; o CharacterController Unity ainda usa uma cápsula de raio 0,4 m, diferença de colisão já registrada em 3A.
- **Compilação:** Unity 6000.6.4f1 concluiu a recompilação sem erros nem avisos. O configurador foi executado novamente depois dos ajustes e os valores foram lidos de volta do prefab/cena.
- **Execução:** o Input System recebeu um clique esquerdo simulado na janela de jogo e executou o primeiro ataque: slime 4→2, animação de recuo ligada e direção de mira atualizada para o octante correspondente ao cursor. Um segundo clique imediato foi bloqueado; depois de aguardar 1,8 s (intervalo configurado de 1,6 s), outro clique levou o slime a 0 de vida. Após 0,38 s, o objeto foi removido. O jogador permaneceu em 12/12. A captura visual também confirmou cenário, jogador e setor de mira. Console final: 0 erros e 0 avisos.
- **Revisão independente:** o subagente de validação conferiu o código e a serialização, apontou dois ajustes de paridade, e confirmou depois das correções que ambos estão salvos e sem bloqueadores.
- **Pendências ao fechar 3C:** XP/abates, aranhas, machado automático, cartas, HUD, áudio, menu e telas finais ficaram para os checkpoints seguintes.

### Checkpoint 3D — Aranha comum e surgimento por ritmo

- **Importação/configuração:** o prefab `Aranha.prefab` usa os cinco quadros de caminhada, os dois de ataque e o sprite de recuo. Os quadros foram recortados dos atlases preservando as margens transparentes usadas pelo Godot. A cena configura o spawner com raio de 15–30 m, atraso de 3 s, limites de 5/10/15/20/30 e intervalos de 3/2/1,5/1/0,75 s.
- **Compilação:** recompilação concluída no Unity 6000.6.4f1, sem falhas. Depois do teste, o Console foi limpo e terminou com 0 erros e 0 avisos.
- **Execução:** um ataque simulado acertou uma aranha, reduziu a vida de 1 para 0 e exibiu o sprite de recuo; o jogador permaneceu com 12/12. Uma chamada controlada ao spawner produziu uma aranha viva de cinco vagas, a 24,42 m do jogador, com alvo configurado. O Play Mode foi encerrado e a cena salva foi reaberta; jogador e spawner estavam ativos e não restaram clones de teste.
- **Revisão visual:** captura do Play Mode inspecionada; cenário 2.5D, personagem e aranhas estavam visíveis. A captura foi diagnóstica e não é imagem de apresentação.
- **Revisão independente:** o subagente conferiu interface de dano, referências do prefab/cena e a correção para uma aranha morta concluir gravidade e recuo quando o jogador também morre. A verificação dinâmica específica desse último caso ficou pendente: a instância temporária foi removida antes de a leitura de posição, então a confirmação é por revisão de código, não por medição em execução.
- **Pendências ao fechar 3D:** a morte ainda não registrava abates nem XP; aranhas pequenas, enxames, progressão, HUD, áudio, menu e estados finais ficaram para os checkpoints seguintes.

### Checkpoint 4A — XP, abates e coleta

- **Importação/configuração:** a joia foi recortada do atlas original do projeto Godot sem alterar a imagem-fonte; o prefab da joia, o total de XP e o estado persistente da partida estão configurados na cena Unity. Slimes concedem 3 XP por abate e aranhas concedem 1.
- **Compilação:** scripts e configurador de progresso compilaram no Unity 6000.6.4f1; a cena foi salva, fechada e reaberta com o componente de estado e a referência do prefab presentes.
- **Execução:** uma morte de slime gerou três joias; atração e coleta elevaram o estado de XP para 3. Uma verificação controlada de 10 XP gerou uma carta pendente e 0 XP restante; registrar uma escolha consumiu a pendência e avançou o próximo requisito para 20 XP. Esses testes cobrem XP e contagem, não o fluxo da interface de cartas.
- **Pendências ao fechar 4A:** cartas ainda não apresentavam opções nem aplicavam efeitos, e o machado automático ainda não estava migrado.

### Checkpoint 4B — HUD de vida, progresso e partida

- **Importação/configuração:** a HUD tem painéis de vida, XP e dados da partida. Uma imagem branca local de 1×1 é importada como Sprite e atribuída às duas imagens `Filled`, permitindo que o UGUI respeite o valor de preenchimento.
- **Compilação:** o configurador e os scripts da HUD compilaram sem falha. A cena reaberta contém os três painéis, as oito referências serializadas da HUD e os dois sprites válidos; revisão independente confirmou a serialização.
- **Execução:** uma captura com vida parcial registrou preenchimento de vida em 0,17 e XP em 0,00; o screenshot mostra 1/12 de vida com uma fração vermelha e XP 0/10 com a barra vazia. A revisão visual também confirmou os valores de tempo, abates e aranhas. Console ao encerrar: 0 erros e 0 avisos.
- **Problema encontrado e corrigido:** uma barra inicialmente usava `Image.Type.Filled` sem Sprite e ficava visualmente cheia. O validador detectou a divergência entre número e imagem; a imagem local foi adicionada e o teste foi repetido com valores parciais.
- **Pendências ao fechar 4B:** a HUD ainda não incluía seleção de cartas nem tela de morte. A escala local do Canvas aparecia como zero no YAML, embora o Unity reabrisse a cena com escala efetiva `(1,1,1)` e renderizasse a interface.

### Checkpoint 3E — Enxame de aranhas e água

- **Importação/configuração:** a cena Unity contém 12 volumes de água invisíveis correspondentes aos volumes exportados da cena Godot. Posição, escala e rotação conferidas; cada volume usa gatilho físico e multiplica a velocidade por 2, com FOV de água em 50°. O prefab separado de aranha pequena usa escala visual 0,45, 1 de vida, 5 de dano de toque e 1 XP por abate. O spawner de enxame foi ligado à cena e conserva os valores definidos pelo script Godot.
- **Compilação:** Unity 6000.6.4f1 concluiu as recompilações sem erros nem avisos. Os configuradores de aranhas e água salvaram a cena e os prefabs; revisão independente confirmou a serialização.
- **Execução:** onda controlada criou 12 aranhas pequenas com velocidade 8, dano 5 e XP 1. Entrada e saída física de água restauraram a velocidade da personagem e da aranha; com melhoria de movimento 1,3, a personagem passou de 6,5 para 2,6 na água e voltou a 6,5 ao sair. A câmera mudou de FOV 75 para 50 e retornou a 75. O contador de áreas sobrepostas foi revisado estaticamente e exercitado com chamadas controladas de entrada/saída; o cenário de sobreposição entre dois volumes não foi medido por deslocamento físico.
- **Diferença conhecida:** a cápsula CharacterController da aranha pequena é cerca de 0,095 m mais estreita no eixo X do que o colisor escalado no Godot. Revisar essa aproximação ao ajustar colisões.

### Checkpoint 4C — Cartas, machado e fluxo de partida

- **Implementação:** seleção e aplicação de cartas, pausa durante a escolha, melhorias de personagem/ataque e machado automático foram ligados ao estado de partida. O estado e o fluxo são resolvidos mesmo quando o singleton de estado ainda não foi atribuído no primeiro acesso.
- **Execução:** a escolha de cartas altera os valores do jogador e retoma o fluxo após a seleção; o machado cria projéteis e segue o alvo. Reinício e retorno ao menu iniciam uma partida limpa. A pausa por Esc, o fim de partida e as transições de música foram exercitados em checkpoints anteriores.
- **Validação independente:** após esta continuação, o validador confirmou que todos os sistemas centrais referenciados pela cena ativa e pelos autoloads têm equivalentes Unity; nenhuma referência serializada do HUD ficou quebrada.

### Checkpoint 5 — Menu, HUD completo e cobertura

- **HUD:** revisão independente apontou que faltavam etapa e a indicação `ESC: PAUSA`. O HUD foi ajustado para incluir `ETAPA`, contagem/limite de slimes e aranhas, abates e pausa. O configurador recriou e salvou os textos `TempoEtapa`, `StatusInimigos` e `StatusPartida` na cena. Os oito campos serializados do HUD apontam para componentes dos tipos esperados.
- **Execução:** Play Mode exibiu `00:11 | ETAPA 1`, `SLIMES 10/15`, `ARANHAS 3/5` e `ESC: PAUSA`. Com o tempo do spawner ajustado em execução para 31 segundos, o HUD passou a Etapa 2 e limite 30 de slimes. Console final da validação: 0 erros e 0 avisos.
- **Filtro CRT:** o shader de tela do menu foi portado para o URP com os mesmos parâmetros usados na cena Godot. A revisão encontrou e corrigiu mapas internos incompletos nos renderizadores; agora PC registra dois recursos (SSAO e CRT) e Mobile registra um. Os dois assets foram reimportados e reabertos, e o Unity encontrou o filtro em ambos. Em Play Mode, `MenuAvalonya` registrou o filtro ativo e `AvalonyaWorld` o registrou desativado. A validação final terminou com zero erros e avisos. A revisão estática independente confirmou as referências dos subassets e a permanência do SSAO. Captura do menu: `outputs/Avalonia-Menu-CRT.png`.
- **Limite deste checkpoint:** o renderizador Mobile foi validado no asset e por reabertura no editor, mas não foi executado em dispositivo ou emulação Mobile.
- **Diferença não bloqueante:** o YAML serializa a escala local do Canvas como `(0,0,0)`, embora o Unity reabra a cena com escala efetiva `(1,1,1)` e exiba o HUD.
- **Revisão final independente:** cobertura de gameplay e referências conferidas estaticamente; a confirmação visual dos novos textos é limitada aos valores observados em execução, sem nova captura de tela nesta revisão.
