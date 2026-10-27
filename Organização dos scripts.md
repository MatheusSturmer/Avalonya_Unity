# Organização dos scripts

Este guia descreve a estrutura da versão Unity para quem vai interpretar, corrigir ou ampliar o jogo. Os scripts de execução ficam em `Assets/Scripts/Runtime`; os scripts em `Assets/Editor` são ferramentas manuais de configuração do projeto. Os nomes e caminhos abaixo são relativos à pasta `Avalonia/`.

### Percurso principal do jogo

1. A build começa em `Assets/Scenes/MenuAvalonya.unity`. `MenuPrincipal` controla menu, opções, créditos, volume, tela cheia e a troca para a partida.
2. A cena de partida é `Assets/Scenes/AvalonyaWorld.unity`. Nela, `Jogador` movimenta o personagem; `VidaJogador` acompanha sua vida; `AtaqueBasico` lê o clique esquerdo; e `MachadoAutomatico` executa a habilidade desbloqueada por progressão.
3. `SpawnerSlime`, `SpawnerAranha` e `SpawnerEnxameAranhas` criam inimigos a partir dos prefabs em `Assets/Prefabs/`. Cada inimigo ativo e vivo entra em `RegistroAlvosDano`; ao morrer, desativar ou ser destruído, sai do registro. Os ataques consultam essa lista, e `ProjetilMachado` continua usando colisão local. Ao morrer, cada inimigo também avisa `EstadoJogo`.
4. O estado da partida contabiliza abates e XP. `JoiaXp` representa o XP no mundo. Ao atingir o necessário, `InterfaceCartas` mostra as opções e `VisualCarta` apresenta cada carta. A escolha retorna ao estado para aplicar a melhoria.
5. `Hud` lê vida, dano, XP, tempo, etapa, inimigos e abates para atualizar a interface. `FluxoPartida` coordena pausa, morte, reinício e retorno ao menu. `AudioJogo` troca a música quando o total de abates alcança o limite configurado.

### Scripts de execução (25 arquivos)

#### Jogador e câmera

| Arquivo | Onde está ligado | Responsabilidade e área que altera |
|---|---|---|
| `Assets/Scripts/Runtime/Jogador.cs` | Componente no objeto `Jogador` em `AvalonyaWorld`. Requer `CharacterController` configurado na cena. | Movimento no plano X/Z, aceleração, atrito, controle no ar, gravidade, derrapagem, recuo e bloqueio de movimento após a morte. Implementa `ICorpoAgua`, portanto a água também altera a velocidade e o campo de visão por meio deste componente. Emite o evento `Pousou` e chama a animação visual. Ajuste aqui a sensação e as regras de movimentação do personagem. |
| `Assets/Scripts/Runtime/VidaJogador.cs` | Componente no mesmo objeto `Jogador` em `AvalonyaWorld`; usa `Jogador` no mesmo GameObject. | Mantém vida atual/máxima, aplica dano e cura, e publica os eventos `VidaAlterada`, `DanoRecebido` e `Morreu`. Ao zerar, avisa o controlador do jogador e dispara o evento que `FluxoPartida` usa para abrir a tela de fim. `Hud`, ataques e spawners também consultam se o jogador morreu. Ajuste aqui os valores e regras de vida; o valor inicial é serializado no componente da cena. |
| `Assets/Scripts/Runtime/AnimacaoJogador.cs` | Componente no objeto filho `VisualJogador` em `AvalonyaWorld`; exige `SpriteRenderer`. | Recorta quadros das texturas configuradas e exibe animações direcionais de parado, caminhada, corrida e dano. Também aplica squash/alongamento ao pousar e derrapar, e gira o sprite para a câmera. Recebe os dados de movimento de `Jogador`. Ajuste aqui a apresentação e os parâmetros visuais; os sprites/quadros referenciados são dados serializados no objeto da cena. |
| `Assets/Scripts/Runtime/CameraFollow.cs` | Componente `CameraFollow` no objeto `CameraPrincipal` em `AvalonyaWorld`; exige `Camera`. | Segue o jogador com mola, antecipa a direção do movimento, espelha suavemente o deslocamento lateral e pode afastar a câmera/aumentar o FOV durante movimento rápido. Também reage ao pouso e aplica o FOV reduzido informado pela água. Gerencia temporariamente o culling invertido para a renderização 2.5D. Ajuste aqui enquadramento, resposta e distância da câmera. |
| `Assets/Scripts/Runtime/AreaAgua.cs` | Em cada volume `Agua`, `Agua2`…`Agua12` em `AvalonyaWorld`. Exige `BoxCollider` e `Rigidbody`; configura-os como trigger e corpo cinemático. | Detecta jogador, slime e aranha que entram/saem do volume. Chama `EntrarNaAgua`/`SairDaAgua` e mantém contagem compartilhada entre volumes sobrepostos. Ajuste aqui os parâmetros comuns de desaceleração e FOV da água; a posição e o tamanho de cada volume são dados da cena, não deste script. |

#### Combate e inimigos

| Arquivo | Onde está ligado | Responsabilidade e área que altera |
|---|---|---|
| `Assets/Scripts/Runtime/AtaqueBasico.cs` | Componente no objeto `Jogador` em `AvalonyaWorld`; requer `Jogador`. | Lê o clique esquerdo pelo Input System, calcula mira com o mouse em oito direções, respeita recarga e percorre `RegistroAlvosDano`, filtrando por alcance, abertura angular e diferença de altura. Causa dano a todos os alvos válidos no setor e desenha a guia/efeito do golpe. Expõe dano e intervalo para a HUD e permite que as cartas alterem dano/velocidade de ataque. Ajuste aqui as regras do golpe básico. |
| `Assets/Scripts/Runtime/MachadoAutomatico.cs` | Componente no objeto `Jogador` em `AvalonyaWorld`; requer `Jogador`. | Consulta `EstadoJogo` e só dispara depois que a carta do machado é desbloqueada. Escolhe os inimigos vivos mais próximos de `RegistroAlvosDano`, sem ultrapassar a quantidade de machados, cria um objeto de projétil por alvo e passa dano, velocidade, alcance e sprites. Mantém a recarga sincronizada quando uma melhoria altera o intervalo. Ajuste aqui seleção de alvos, posição de lançamento e parâmetros do projétil. |
| `Assets/Scripts/Runtime/ProjetilMachado.cs` | Não está salvo como componente em cena/prefab: `MachadoAutomatico` cria um GameObject e adiciona este componente durante o jogo. | Move e anima cada machado, detecta colisões por esfera e aplica dano uma vez a cada inimigo atingido. Inimigos sobreviventes também recebem impulso de recuo. O projétil desaparece ao alcançar sua distância máxima, se o autor morrer ou deixar de existir. Ajuste aqui deslocamento, detecção, animação, dano por acerto e término do voo. |
| `Assets/Scripts/Runtime/IAlvoDano.cs` | Não é componente. Contém três contratos: `IAlvoDano`, `IRecuoMachado` e `ICorpoAgua`. | Define as funções que permitem ao ataque e aos projéteis identificar e ferir um alvo; permite ao machado aplicar recuo; e permite às áreas de água aplicar/remover o efeito. É implementado pelos inimigos; `Jogador` implementa o contrato de água. Ao criar uma nova classe de inimigo, implemente os contratos pertinentes para integrá-la a combate, machado e água. |
| `Assets/Scripts/Runtime/RegistroAlvosDano.cs` | Classe estática, não é componente de cena. Slimes e aranhas chamam o registro ao ativar e removem-se ao morrer, desativar ou destruir. | Guarda apenas alvos ativos e vivos, evita duplicatas e remove referências inválidas. É limpo ao iniciar um novo runtime. Os ataques percorrem a lista registrada sem fazer uma busca global por componentes. Ao criar um inimigo que implemente `IAlvoDano`, conecte seu ciclo de ativação e morte a este registro. |
| `Assets/Scripts/Runtime/InimigoSlime.cs` | Componente no prefab `Assets/Prefabs/Slime.prefab`; instanciado por `SpawnerSlime`. | Controla perseguição, separação entre slimes, gravidade, cabeçada (preparação, investida, tentativa de acerto e recarga), vida, dano, animação e recuo. Ao morrer, gera o evento usado para registrar abate/XP e permanece pelo tempo necessário para concluir o efeito visual antes de ser destruído. Ajuste aqui comportamento, atributos e ataque do slime; o prefab guarda os valores e sprites concretos. |
| `Assets/Scripts/Runtime/InimigoAranha.cs` | Componente nos prefabs `Assets/Prefabs/Aranha.prefab` e `Assets/Prefabs/AranhaPequena.prefab`; criado pelos spawners de aranha. | Controla perseguição, ataque de contato/investida, vida, dano, animação, recuo, morte e XP. O método `ConfigurarValoresEnxame` permite ao spawner mudar velocidade e dano das aranhas pequenas. Ajuste aqui comportamento individual da aranha; valores de prefab e dos enxames também vêm dos respectivos assets/spawners. |

#### Spawning, XP e progressão

| Arquivo | Onde está ligado | Responsabilidade e área que altera |
|---|---|---|
| `Assets/Scripts/Runtime/SpawnerSlime.cs` | Objeto `SlimeSpawn` em `AvalonyaWorld`; referencia jogador e prefab `Slime`. | Cria slimes em posições aleatórias entre raios ao redor do jogador. Controla atraso, intervalo e limite por etapa com base no tempo da partida; encerra surgimentos se o jogador morrer. Fornece tempo, etapa, quantidade viva e limite para `Hud`. Ajuste aqui o ritmo e a densidade dos slimes. |
| `Assets/Scripts/Runtime/SpawnerAranha.cs` | Objeto `AranhaSpawn` em `AvalonyaWorld`; referencia jogador e prefab de aranha. | Cria aranhas comuns em volta do jogador, aumenta a pressão por etapas e acompanha o número vivo. Para quando o jogador morre. A HUD consulta contagem e limite deste spawner. Ajuste aqui o ritmo e a densidade das aranhas comuns. |
| `Assets/Scripts/Runtime/SpawnerEnxameAranhas.cs` | Objeto `EnxameAranhasSpawn` em `AvalonyaWorld`; referencia jogador e prefab `AranhaPequena`. | Dispara grupos de aranhas pequenas após atraso e em intervalos. Define um centro ao redor do jogador, espalha os membros e calcula o tamanho da onda com base nas cartas selecionadas, respeitando limites. Também passa velocidade e dano específicos aos inimigos do enxame. Ajuste aqui tamanho, frequência, dispersão e força do enxame. |
| `Assets/Scripts/Runtime/EstadoJogo.cs` | Componente no objeto `EstadoJogo` em `AvalonyaWorld`; contém referência ao prefab de XP e dados das cartas. | Mantém o singleton da partida, XP, abates, cartas pendentes/selecionadas e atributos melhoráveis. Ao receber uma morte, incrementa abates e instancia joias de XP. Ao recolher XP suficiente, sorteia opções; ao selecionar uma carta, valida disponibilidade e aplica seus efeitos ao jogador, vida, ataque ou machado. `Hud`, os spawners, áudio e interface leem esse estado. Ajuste aqui regras de progressão, efeitos e dados/requisitos das cartas. |
| `Assets/Scripts/Runtime/CartaProgressao.cs` | Não é componente de cena; é um tipo de dado serializado na lista de cartas de `EstadoJogo`. | Guarda `id`, título, descrição e imagem. O `id` conecta o conteúdo visual à regra de efeito tratada por `EstadoJogo`; mantenha os identificadores consistentes ao editar/adicionar cartas. |
| `Assets/Scripts/Runtime/JoiaXp.cs` | Componente no prefab `Assets/Prefabs/JoiaXp.prefab`; instanciado por `EstadoJogo` quando inimigos morrem. | Faz a joia saltar, esperar o jogador entrar no raio de atração, voar até ele e concluir a coleta com efeito visual/luz. Depois informa a quantidade coletada ao estado da partida. Ajuste aqui movimento/atração e visual da coleta; aparência e componentes visuais também estão no prefab. |

#### Interface, menu, áudio e fluxo

| Arquivo | Onde está ligado | Responsabilidade e área que altera |
|---|---|---|
| `Assets/Scripts/Runtime/Hud.cs` | Objeto `HUD Avalonya` em `AvalonyaWorld`; campos de texto e imagens são referências serializadas da cena. | Atualiza preenchimentos e números de vida/XP, dano, tempo, etapa, slimes, aranhas e abates; exibe também a dica de pausa por Esc. Lê `VidaJogador`, `AtaqueBasico`, `EstadoJogo` e os dois spawners principais. Se mudar campos ou nomes de UI, atualize tanto as referências do componente na cena quanto o configurador `ConfigurarHud`. |
| `Assets/Scripts/Runtime/InterfaceCartas.cs` | Objeto `Interface de Cartas Avalonya` em `AvalonyaWorld`; referencia painel, títulos e componentes visuais de carta. | Observa quando o estado tem escolha pendente, pausa o tempo, preenche opções nos visuais e recebe o `id` escolhido. Depois confirma a seleção no estado e fecha/retoma a partida. Ajuste aqui o momento de abrir, fechar e selecionar; hierarquia, botões e aparência ficam na cena e nos objetos `CartaVisual_*`. |
| `Assets/Scripts/Runtime/VisualCarta.cs` | Em `CartaVisual_1`, `CartaVisual_2`, `CartaVisual_3` em `AvalonyaWorld`. Cada objeto liga imagem, textos e botão. | Apresenta uma carta individual: sprite, título e descrição. Substitui os listeners do botão para enviar o identificador da opção selecionada à interface. Ajuste aqui a apresentação individual; layout e componentes UGUI são dados da cena. |
| `Assets/Scripts/Runtime/FluxoPartida.cs` | Objeto `Fluxo de Partida Avalonya` em `AvalonyaWorld`; referências para painel, textos e botões de fluxo. | Lê pausa, controla `Time.timeScale`, reage ao evento de morte e exibe painel de pausa/fim de jogo. Liga os botões para continuar, reiniciar, voltar ao menu ou sair. Ajuste aqui transições e comandos; textos, visibilidade e referências dos botões ficam na cena. |
| `Assets/Scripts/Runtime/MenuPrincipal.cs` | Objeto `MenuAvalonya` em `Assets/Scenes/MenuAvalonya.unity`; referencia painéis, botões, slider, toggle e áudio do menu. | Alterna painéis principal/opções/créditos, inicia `AvalonyaWorld`, define volume e tela cheia e encerra o aplicativo. Também chama `ControleEfeitoCrt` para ligar o CRT no menu e desligá-lo ao sair. Ajuste aqui ações e opções; a composição do menu fica na cena. |
| `Assets/Scripts/Runtime/AudioJogo.cs` | Objeto `Audio da Partida` em `AvalonyaWorld`; possui duas referências `AudioSource`. | Toca em loop as faixas inicial e frenética. Inscreve-se no evento de abates de `EstadoJogo` e faz uma transição suave quando chega a 80 abates. Também pausa o áudio global quando o tempo do jogo está pausado. Ajuste aqui o limiar e a lógica de transição; clipes e AudioSources ficam configurados na cena. |
| `Assets/Scripts/Runtime/ControleEfeitoCrt.cs` | Classe estática, não é componente de cena. É chamada por `MenuPrincipal`. | Percorre os renderizadores URP ativos e liga/desliga o recurso de tela cheia chamado `CRT Menu Avalonya`. Ajuste aqui o nome esperado ou o método de ativação do efeito; o shader/recurso em si está nos assets de renderização e configurações URP, não neste arquivo. |

### Ferramentas do Unity Editor (12 arquivos)

Os scripts em `Assets/Editor/` aparecem como comandos manuais sob `Avalonya/Migracao`. Eles configuram/salvam cenas, prefabs e importação de sprites; não são componentes que rodam a cada quadro. Antes de executar um comando de configuração, confira seu código e as alterações que pretende gravar.

| Arquivo | Comando do menu | O que configura ou pode alterar |
|---|---|---|
| `Assets/Editor/GeradorCenarioInicial.cs` | `Gerar cenario base`; `Ajustar cenario existente`; `Importar objetos do Godot` | Gera/ajusta o cenário e importa objetos da origem Godot. Pode alterar a cena de jogo e os objetos ambientais. |
| `Assets/Editor/ConfigurarJogador.cs` | `Configurar personagem e camera` | Prepara personagem, componentes de movimento/animação e câmera na cena, incluindo referências de sprites. |
| `Assets/Editor/ConfigurarAtaqueBasico.cs` | `Configurar ataque basico e recuo do slime` | Configura ataque do jogador e elementos do prefab de slime relacionados a dano/recuo. |
| `Assets/Editor/ConfigurarCombateSlimes.cs` | `Configurar vida e cabeçada do slime` | Ajusta vida, ataque e relações do slime/jogador usadas no combate. |
| `Assets/Editor/ConfigurarSlimes.cs` | `Configurar slime e surgimento` | Prepara prefab, sprites, valores e spawner dos slimes. |
| `Assets/Editor/ConfigurarAranhas.cs` | `Configurar aranha comum e surgimento` | Prepara animações/prefabs de aranhas e configura o surgimento comum e pequeno. |
| `Assets/Editor/ConfigurarAgua.cs` | `Configurar volumes de agua` | Cria/ajusta volumes de água e seus colliders/transformações na cena. |
| `Assets/Editor/ConfigurarProgressao.cs` | `Configurar base de XP e abates` | Prepara os dados e objetos de XP, abates e prefab da joia. |
| `Assets/Editor/ConfigurarCartas.cs` | `Configurar cartas e machado automatico` | Configura as cartas, dados de progressão, sprites e habilidade de machado no jogador. |
| `Assets/Editor/ConfigurarHud.cs` | `Configurar HUD de vida e progresso` | Cria/ajusta painéis, barras e textos da HUD. Pode reconstruir objetos da interface em `AvalonyaWorld`. |
| `Assets/Editor/ConfigurarFluxo.cs` | `Configurar menu, pausa e audio` | Prepara menu, telas de pausa/fim, sistema de eventos e fontes/faixas de áudio nas cenas. |
| `Assets/Editor/ConfigurarEfeitoCrt.cs` | `Configurar filtro CRT do menu` | Configura o recurso CRT nos renderizadores URP do projeto. |

### Cenas, prefabs e configuração

- `Assets/Scenes/MenuAvalonya.unity`: cena inicial de build, com `MenuPrincipal` no objeto `MenuAvalonya`.
- `Assets/Scenes/AvalonyaWorld.unity`: cena de jogo ativa, com jogador, câmera, HUD, estado, fluxo, música, spawners, interface de cartas e volumes de água.
- `Assets/Scenes/SampleScene.unity`: cena de exemplo do Unity; não é uma das duas cenas habilitadas na build atual.
- `Assets/Prefabs/Slime.prefab`: inimigo slime padrão e seus valores visuais/físicos.
- `Assets/Prefabs/Aranha.prefab`: aranha comum.
- `Assets/Prefabs/AranhaPequena.prefab`: variante usada nos enxames.
- `Assets/Prefabs/JoiaXp.prefab`: objeto de coleta de XP.
- `ProjectSettings/EditorBuildSettings.asset`: ordem atual da build — menu primeiro, mundo de jogo depois.
- `Packages/manifest.json`: dependências do projeto Unity, incluindo URP e Input System.
- `Docs/MIGRACAO.md`: histórico detalhado de migração e checkpoints de validação. Use-o como contexto histórico; confirme sempre o comportamento atual no código e na cena.

### Ao alterar o jogo

- Para mudar **movimento, dano recebido ou reação do jogador**, comece por `Jogador.cs` e/ou `VidaJogador.cs`; confira também o objeto `Jogador` em `AvalonyaWorld` e a animação em `VisualJogador`.
- Para mudar **ataque manual**, comece por `AtaqueBasico.cs`; confira o componente e os sprites/valores serializados no objeto do jogador. Para alvo e dano dos inimigos, revise também `IAlvoDano.cs` e o script do inimigo correspondente.
- Para mudar **machado**, siga a cadeia `EstadoJogo.cs` → `MachadoAutomatico.cs` → `ProjetilMachado.cs`; as cartas habilitam e ajustam os valores, o jogador guarda o componente, e o projétil é criado em runtime.
- Para mudar **vida, ataque, animação ou XP concedido de um inimigo**, edite o script do tipo e confira também o prefab correspondente. O spawner controla quando/onde o prefab é instanciado.
- Para mudar **quantidade ou dificuldade de inimigos**, edite o spawner apropriado e confira suas referências na cena. O HUD lê limites/contagens desses spawners.
- Para mudar **XP, requisitos de nível, cartas e efeitos**, confira `EstadoJogo.cs`, `CartaProgressao.cs` e `ConfigurarCartas.cs`; revise os dados serializados em `AvalonyaWorld` depois de qualquer mudança de identificador/campo.
- Para mudar **HUD ou telas**, altere o script controlador e os objetos/referências da cena. Os comandos `ConfigurarHud` e `ConfigurarFluxo` podem recriar ou salvar a interface, portanto verifique o diff da cena após executá-los.
- Para mudar **água**, o comportamento está em `AreaAgua.cs` e os volumes estão individualmente posicionados na cena. Não confunda o valor global de água com a posição/tamanho de cada volume.
- Para mudar **música**, edite `AudioJogo.cs` e os AudioSources/clipes no objeto `Audio da Partida`. A música do menu está em `MenuPrincipal` e na cena de menu.

### Regras de manutenção importantes

- Preserve cada `.meta` junto do arquivo correspondente. O GUID do `.meta` é como cenas, prefabs e outros assets encontram um script; recriar um `.meta` pode quebrar vínculos serializados.
- Scripts anexados e campos `[SerializeField]` têm referências guardadas em YAML nas cenas/prefabs. Renomear classe, arquivo, campo serializado ou GameObject pode quebrar referências ou exigir uma migração explícita.
- Mudanças de campos serializados devem ser verificadas no componente correspondente dentro do Unity e nos assets afetados, sobretudo `AvalonyaWorld.unity` e os prefabs.
- Os scripts em `Assets/Editor` são capazes de gravar assets. Leia o comando antes de executá-lo e confira quais cenas/prefabs ele salva.
- A cena de exemplo não representa o fluxo principal. Para conferir os sistemas de jogo, acompanhe a cena de menu e a `AvalonyaWorld`.
- Este documento descreve o arranjo encontrado nos arquivos do projeto. Depois de mudanças estruturais, atualize os caminhos e os vínculos documentados aqui.
