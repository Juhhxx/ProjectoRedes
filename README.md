# SRJ - Projecto Final

## Autoria

* Júlia Costa - a22304403

## Link para Repositório Git e Build

* [Link para o Repositório Git](https://github.com/Juhhxx/ProjectoRedes)

* [Link para uma Build do Projecto](https://juhxx-x.itch.io/netpet-battlerz)

## Netpet Battlerz

O conceito base do jogo é ser um *monster battler* online, baseado em *Pokemon*, onde jogadores podem escolher entre 3 criaturas (e entre 10 ataques para cada), para batalharem uns contra os outros em batalhas 1 v 1 em turnos.

Durante o decorrer de uma batalha, cada jogador deve escolher um ataque a realizar (estes podem ser ataques fisicos, ou booster/nerfers de stats), depois estes são realizados por ordem, baseado na *speed* da criatura que efectuou o ataque. Depois este *loop* é repetido até uma das criaturas tiver 0 HP.

Estas batalhas podem ser privadas, onde o jogador cria/junta-se a uma sessão privada através de um código, ou públicas, onde um jogador é pareado com outro baseado no seu EXP (de forma a juntar jogadores com niveis próximos).

## Relatório

explicar implemetação, scripts, funcionamento de cada um e proposito

explicar como funciona a coneção online e como a fiz

explicar matchmaking e os valores que o definem

diagram de redes, relay server, host, clients

## Como Testar o Jogo

Para testar o decorrer de uma batalha, o professor pode realizar uma batalha privada, abrindo duas cópias do jogo e :

* Na primeira, clicar no botão "Host Battle" e "Host";

* Na outra, clicar no botão "Host Battle" e "Join", depois escrever o *join code* que vai aparecer na primeira cópia, e por fim carregar no botão "Connect";

* Denovo na primeira cópia, carregar no botão "Start" depois de ser mostrado que 2 jogadores estão conectados.

Para testar o **Matchmaking**, o professor pode utilizaro  cheat `Ctrl + P` ou `Ctrl + O` para aumentar/diminuir respectivamente o EXP do seu jogador por 500 pontos, depois só precisa de clicar no botão "Find Battle" em duas ou mais cópias do jogo.

> **Nota**\
O funcionamento do decorrer das batalhas não se encontra completo :\
\
Existem problemas na sincronização da UI (sendo que o jogador que serve de *Host* tem controlo maximo sobre quando o UI é mudado, causado alguma desincronização quando os jogadores passam o texto em tempos diferentes);\
\
Não existe a lógica para sair de uma batalha quando esta acaba.

## Bibliografia

* [Unity Discussions | Is Matchmaking Compatibale with Relay](https://discussions.unity.com/t/is-matchmaking-compatible-with-relay/895231/8)

* [Unity Discussions | How to Initialize Relay from Matchmaked Session](https://discussions.unity.com/t/how-to-initialize-relay-from-matchmaked-session/1651253/5)

* [Unity Discussions | Matchmaker Client-Hosting with Multiplayer problem](https://discussions.unity.com/t/matchmaker-client-hosting-with-multiplayer-problem/1657903)

* [Unity Discussions | How to use RelayServerData](https://discussions.unity.com/t/how-to-use-relayserverdata/1547792/2)

* [Unity Discussions | Imported Package not being recognized in Unity or VS2019](https://discussions.unity.com/t/imported-package-not-being-recognized-in-unity-or-vs2019/895488/6)

* [Youtube | COMPLETE Unity Multiplayer Tutorial (Netcode for Game Objects) - Code Monkey](https://www.youtube.com/watch?v=3yuBOB3VrCk)

* [Youtube | Play Online Together Using Relay || Unity Tutorial - Freedom Coding](https://www.youtube.com/watch?v=DXsmhMMH9h4)

* [Youtube | Unity Store Data in Playfab | Interacting with data in PlayFab - Skye Games](https://www.youtube.com/watch?v=KoWpVuta_nE)

* [Youtube | Playlist das Aulas de SRJ 2024/25 - Diogo Andrade](https://www.youtube.com/playlist?list=PLheBz0T_uVP3JaTA4wMs38MgOKiIpDpLG)

* [Moodle | Project MPWyzards usado em Aula - Diogo Andrade](https://moodle.ensinolusofona.pt/mod/resource/view.php?id=441486)

* [Chat GPT | Diversas perguntas sobre como estruturar/resolver problemas no meu código](https://chatgpt.com/share/685f3d20-ef80-8007-ac4e-bbbb51a932e6)

## Agradecimentos

* Mafalda Pinto - Ajuda no conceito e design do jogo (personagens, ataques, etc...)
