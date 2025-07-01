# SRJ - Projecto Final

## Autoria

* **Júlia Costa - a22304403**

## Link para Repositório Git e Build

* [Link para o Repositório Git](https://github.com/Juhhxx/ProjectoRedes)

* [Link para uma Build do Projecto](https://juhxx-x.itch.io/netpet-battlerz)

## Netpet Battlerz

O conceito base do jogo é ser um *monster battler* online, baseado em *Pokemon*, onde jogadores podem escolher entre 3 criaturas (e entre 10 ataques para cada), para batalharem uns contra os outros em batalhas 1 v 1 por turnos.

Durante o decorrer de uma batalha, cada jogador deve escolher um ataque a realizar (estes podem ser ataques fisicos, ou booster/nerfers de stats), depois estes são realizados por ordem, baseado na *speed* da criatura que efectuou o ataque. Depois este *loop* é repetido até uma das criaturas ter 0 HP.

Estas batalhas podem ser privadas, onde o jogador cria/junta-se a uma sessão privada através de um código, ou públicas, onde um jogador é pareado com outro baseado no seu Level (de forma a juntar jogadores com níveis próximos).

## Relatório

### Funcionamento do Jogo

O funcionamento das batalhas é simples, no início cada jogador é apresentado com o menu de ações, onde podem escolher atacar ou ver primeiro os *stats* da sua criatura.

![a](Images/BattleStart.png)

Quando o jogador entra no menu de ataques é apresentado com os ataques que selecionou para a sua criatura, escolhendo um deles, e fica à espera que o seu oponente faça o mesmo.

![a](Images/BattleAttackBtt.png)

![a](Images/BattleAttackSelect.png)

Quando os dois jogadroes já escolheram o seu ataque, os mesmos são efectuados em ordem (esta decidida através do *stat* de *speed* das criaturas).

![a](Images/Movie_006.gif)

No final ganha o jogador que derrotar a criatura do seu oponente.

Os ataques de cada criatura são calculados de forma a imitar os dos jogos *Pokemon* da primeira geração [^1], e seguem o seguinte cálculo.

```c#
private float CalculateDamage(Attack attack)
{
    float rnd = damageRandom.Next(217, 255);
    rnd /= 255;

    float damage = (((((2 * attack.Attacker.Owner.Level * attack.CriticalChance()) / 5)
                    * attack.Power * (attack.Attacker.Attack / Defense)) / 50) + 2)
                    * attack.GetSTAB() * attack.GetEffectiveness(Type) * rnd;

    return Mathf.Ceil(damage);
}
```

\
Tanto criaturas, como ataques, possuem tipos que interagem entre si. isto pode aumentar ou diminuir a quantidade de dano dada por ataques de certos tipo a criaturas de outros, seguindo a seguinte configuração :

![a](Images/DiagramaTipos.png)

### Networking

Para a implementação de jogos online com matchmaking e login, comecei por fazer uma lista dos passos que precisava de realizar, organizei-a por grau aparente de dificuldade de cada tarefa. A lista foi a seguinte :

1. Implementação de um menu de *Login* que permitisse aos jogadores criar ou entrar num conta;

2. Implmentação de um sistema que salva-se informações sobre os jogadores numa base de dados e que fize-se a gestão das suas contas;

3. Implementação de batalhas privadas entre jogadores usando *Join Codes*;

4. Implementação de batalhas públicas online com *matchmaking*.

### Matchmaking

O sistema de matchmaking do jogo é feito usando o serviço **Matchmaker** do **Unity**, e é baseado no nível de cada jogador, sendo o objectivo agrupar jogadores com níveis proximos.

Os níveis dos jogadores são calculados através do seu `EXP` utilizando a seguinte fórmula :

$$ Level = floor( log{_2}( \frac{EXP}{10} + 1) ) $$

Representação gráfica da equação referida :

![a](Images/LevelCalculationGraph.png)

Para fazer o setup do **Matchmaker** criei primeiro uma *Queue* chamada `PlayerLV`, que apenas cria *tickets* para um máximo de 2 jogadores.

Depois criei uma *Pool* dentro dela, chamada `Default`, com um  *Timeout* de 60 segundos e configurada para funcionar apenas para *Client Hosting*.

Dentro da *Pool* criei um set de regras que determinam que :

* Apenas pode ser criada 1 *"Team"*  por jogo;

* Cada *"Team"* tem de ter 2 jogadores, nem mais nem menos;

* Na criação de *"Teams"* os jogadores são agrupados com outros cuja a diferença entre *Level* seja menor que 5.

Estas regras podem ser lidas no seguinte ficheiro *Json* :

```json
{
  "Name": "Normal",
  "MatchDefinition": {
    "Teams": [
      {
        "Name": "Battle",
        "TeamCount": {
          "Min": 1,
          "Max": 1
        },
        "PlayerCount": {
          "Min": 2,
          "Max": 2
        },
        "TeamRules": [
          {
            "Name": "Level_Range",
            "Type": "Difference",
            "Source": "Players.CustomData.LV",
            "Reference": 5,
            "Not": false,
            "EnableRule": true,
            "Relaxations": []
          }
        ]
      }
    ],
    "MatchRules": []
  },
  "BackfillEnabled": false
}
```

### Diagrama de Redes

\
![a](Images/DiagramRedes.png)

## Como Testar o Jogo

Para testar o decorrer de uma batalha, o professor pode realizar uma batalha privada, abrindo duas cópias do jogo e :

* Na primeira, clicar no botão "Host Battle" e "Host";

![a](Images/GameStartHost.png)

![a](Images/GameStartHostBtt.png)

![a](Images/GameStartHostMenuH.png)

* Na outra, clicar no botão "Host Battle" e "Join", depois escrever o *join code* que vai aparecer na primeira cópia, e por fim carregar no botão "Connect";

![a](Images/GameStartHost.png)

![a](Images/GameStartHostBtt2.png)

![a](Images/GameStartMenuC.png)

* Denovo na primeira cópia, carregar no botão "Start" depois de ser mostrado que 2 jogadores estão conectados.

![a](Images/GameStartBattle.png)

Para testar o **Matchmaking**, o professor pode utilizaro  cheat `Ctrl + P` ou `Ctrl + O` para aumentar/diminuir respectivamente o EXP do seu jogador por 500 pontos, depois só precisa de clicar no botão "Find Battle" em duas ou mais cópias do jogo.

![a](Images/GameStartMatch.png)

> **Nota**\
O funcionamento do decorrer das batalhas não se encontra completo :\
\
Existem problemas na sincronização da UI (sendo que o jogador que serve de *Host* tem controlo máximo sobre quando o UI é mudado, causando alguma desincronização quando os jogadores passam o texto em tempos diferentes);

## Bibliografia

* [Unity Discussions | Is Matchmaking Compatibale with Relay](https://discussions.unity.com/t/is-matchmaking-compatible-with-relay/895231/8)

* [Unity Discussions | How to Initialize Relay from Matchmaked Session](https://discussions.unity.com/t/how-to-initialize-relay-from-matchmaked-session/1651253/5)

* [Unity Discussions | Matchmaker Client-Hosting with Multiplayer problem](https://discussions.unity.com/t/matchmaker-client-hosting-with-multiplayer-problem/1657903)

* [Unity Discussions | How to use RelayServerData](https://discussions.unity.com/t/how-to-use-relayserverdata/1547792/2)

* [Unity Discussions | Imported Package not being recognized in Unity or VS2019](https://discussions.unity.com/t/imported-package-not-being-recognized-in-unity-or-vs2019/895488/6)

* [Youtube | COMPLETE Unity Multiplayer Tutorial (Netcode for Game Objects) - Code Monkey](https://www.youtube.com/watch?v=3yuBOB3VrCk)

* [Youtube | Easy MATCHMAKING in Unity! (Skill based, Platform, Region - Tutorial) - Code Monkey](https://www.youtube.com/watch?v=90Iw1aNbSYE)

* [Youtube | Play Online Together Using Relay || Unity Tutorial - Freedom Coding](https://www.youtube.com/watch?v=DXsmhMMH9h4)

* [Youtube | Unity Store Data in Playfab | Interacting with data in PlayFab - Skye Games](https://www.youtube.com/watch?v=KoWpVuta_nE)

* [Youtube | Playlist das Aulas de SRJ 2024/25 - Diogo Andrade](https://www.youtube.com/playlist?list=PLheBz0T_uVP3JaTA4wMs38MgOKiIpDpLG)

* [Moodle | Project MPWyzards usado em Aula - Diogo Andrade](https://moodle.ensinolusofona.pt/mod/resource/view.php?id=441486)

* [Chat GPT | Diversas perguntas sobre como estruturar/resolver problemas no meu código](https://chatgpt.com/share/685f3d20-ef80-8007-ac4e-bbbb51a932e6)

## Agradecimentos

* **Mafalda Pinto -** Ajuda no conceito e design do jogo (personagens, ataques, etc...)

## Referências

[^1]: [Damage Calculation : Generation I - Bulbapedia](https://bulbapedia.bulbagarden.net/wiki/Generation_I)
