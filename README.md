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
Tanto criaturas, como ataques, possuem tipos que interagem entre si. Isto pode afectar a quantidade de dano dada por ataques de certos tipo a criaturas de outros, seguindo a seguinte configuração :

![a](Images/DiagramaTipos.png)

### Networking

Para a implementação de jogos online com matchmaking e login, comecei por fazer uma lista dos passos que precisava de realizar e organizei-a por grau de dificuldade aparente de cada tarefa. A lista foi a seguinte :

1. Implementação de um menu de *Login* que permitisse aos jogadores criar ou entrar numa conta;

2. Implementação de um sistema que salvasse informações sobre os jogadores numa base de dados e que fizesse a gestão das suas contas;

3. Implementação de batalhas privadas entre jogadores usando *Join Codes*;

4. Sincronização da informação entre jogadores e funcionamento das batalhas em modo online;

5. Implementação de batalhas públicas online com *matchmaking*.

#### Login e Criação de Contas

Para fazer um menu de login, comecei por pesquisar online sobre como proceder e encontrei vários tutoriais que recomendavam a intergração do **[Playfab](https://learn.microsoft.com/en-us/gaming/playfab/sdks/unity3d/)** com o **Unity**.

Depois de ver alguns tutorias, comecei a fazer a implementação do meu sistema de gestão de contas. Criei um script chamado `Account Manager` e escrevi os seguintes métodos utilizando a SDK do **Playfab** para o **Unity** :

```c#
public void CreateAccount(string username, string password, Action success = null, Action<string> fail = null)
{
    PlayFabClientAPI.RegisterPlayFabUser(
        new RegisterPlayFabUserRequest()
        {
            Username = username,
            Password = password,

            RequireBothUsernameAndEmail = false,
        },
        response =>
        {
            Debug.Log($"Successfull Account Creation : {username}, {password}");
            LogIntoAccount(username, password, success, fail);
        },
        error =>
        {
            Debug.Log($"Unsuccessfull Account Creation : {username}, {password}\n{error.ErrorMessage}");
            fail(error.ErrorMessage);
        }
    );
}
public void LogIntoAccount(string username, string password, Action success = null, Action<string> fail = null)
{
    PlayFabClientAPI.LoginWithPlayFab(
        new LoginWithPlayFabRequest()
        {
            Username = username,
            Password = password,
        },
        response =>
        {
            Debug.Log($"Successful Account Login for {username}");
            _isLoggedIn = true;
        },
        error =>
        {
            Debug.Log($"Unsuccessful Account Login for {username}\n{error.ErrorMessage}");
            fail(error.ErrorMessage);
        }
    );
}
```

\
Como podemos ver criei dois métodos :

* O método `CreateAccount()`, que recebe um *username* e uma *password* e envia um *request* através da SDK do **Playfab** para criar uma conta com os mesmos parâmetros;

* O método `LogIntoAccount()`, que também recebe um *username* e uma *password*, mas envia um *request* ao **Playfab** para realizar o login numa conta.

Este código, juntamente com outro *script* que controla a parte do UI, já me permitiu fazer o login e criação de contas sem problemas.

#### Gestão de Dados dos Jogadores

Para isto também decidi utilizar a SDK do **Playfab**, visto que vi que a mesma disponibilizava, por cada conta registada, um conjunto de *PlayerData* que me permitia guardar informações sobre os jogadores. Estes dados podiam ser passados e guardados através do Unity como um dicionário do tipo `Dictionary<string,string>`, e mais tarde, lidos e usados para dar *setup* aos valores necessários.

Com isto em mente, e ajuda de um tutorial, escrevi os seguintes métodos no script `Account Manager` :

```c#
private static Dictionary<string, UserDataRecord> _userData;
private static bool _isGettingData;

private void SaveData(Dictionary<string, string> data, Action<UpdateUserDataResult> onSuccess, Action<PlayFabError> onFail)
{
    PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
    {
        Data = data,
    },
    result =>
    {
        if (_userData != null)
        {
            foreach (var key in data.Keys)
            {
                UserDataRecord value = new UserDataRecord { Value = data[key] };

                if (_userData.ContainsKey(key)) _userData[key] = value;
                else _userData.Add(key, value);
            }
        }

        onSuccess(result);
    },
    onFail);
}
private void GetData(Action<GetUserDataResult> onSuccess, Action<PlayFabError> onFail)
{
    while (_isGettingData) Task.Delay(100);

    if (_userData != null)
    {
        onSuccess(new GetUserDataResult() { Data = _userData });
        return;
    }

    _isGettingData = true;

    PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
    result =>
    {
        _userData = result.Data;
        _isGettingData = false;
        onSuccess(result);
    },
    fail =>
    {
        _isGettingData = false;
        onFail(fail);
    });
}
```

\
Como podemos ver criei duas variáveis e dois métodos :

* A variável `_userData` do tipo `Dictionary<string,UserDataRecord>`, que mantem um registo dos valores que estão a ser guardados de forma a não precisar de estar sempre a fazer *requests* para os obter;

* A variável `_isGettingData` do tipo `bool`, que diz se o método `GetData()` está atualmente a colectar dados do **Playfab** ou não;

* O método `SaveData()`, que recebe um dicionário do tipo `Dictionary<string,string>` e manda um *request* ao **Playfab** para guardar os dados enviados. Quando este *request* é executado com sucesso, o método verifica se já existe um dicionário disponível na variável `_userData`, se sim adiciona todos os valores novos e atualiza os que já existiam, depois chama o *delegate* `onSuccess`, que é um parâmetro do próprio método, com o resultado. Caso o *request* falhe, é chamado o *delegate* `onFail`, também este definido como parâmetro do método;

* O método `GetData()`, que começa por verificar se o mesmo já está em execução (observando o valor da variável `_isGettingData`), caso esteja, faz um *delay* de 100 milisegundos e verifica denovo, caso não esteja a ser executado, verifica se já existe um dicionário disponível na variável `_userData`, se sim chama o *delegate* `onSuccess` e passa-lhe os resultados como o `_userData`. Caso tudo acima seja falso, o método muda o valor da variável `_isGettingData` para `true` e começa o processo de pedir os dados necessários ao **Playfab**, através, denovo, de um *request*. Se este *request* for executado com sucesso, a variável `_userData` passa a ter o valor dos dados obtidos pelo resultado do *request*, o valor de `_isGettingData` passa a `false` e é chamado o *delegate* `onSuccess`, definido como parâmetro do método, com o resultado. Se o *request* falhar, o valor de `_isGettingData` passa a `false` e o *delegate* `onFail`, também definido como parâmetro do método, é chamado com o erro emitido.

#### Batalhas Privadas

Para a implementação de batalhas online privadas tive duas implementações, uma utilizando apenas o **Netcode for GameObjects** com conexões via LAN, e outra com o **Netcode  for GameObjects** + **Relay** que já permitia conexões online e sem problemas com a *firewall*. Para o projecto final só usei a segunda abordagem, mas vou falar das duas de forma a expor todo o processo do desenvolvimento do jogo.

### Matchmaking

O sistema de matchmaking do jogo é feito usando o serviço **Matchmaker** do **Unity**, e é baseado no nível de cada jogador, sendo o objectivo agrupar jogadores com níveis proximos.

Os níveis dos jogadores são calculados através do seu `EXP` utilizando a seguinte fórmula :

$$ Level = floor \left( log{_2} \left( \frac{EXP}{10} + 1 \right) \right) $$

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

Para testar o **Matchmaking**, o professor pode utilizar o  cheat `Ctrl + P` ou `Ctrl + O` para aumentar/diminuir respectivamente o EXP do seu jogador por 500 pontos, depois só precisa de clicar no botão "Find Battle" em duas ou mais cópias do jogo.

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

* [Youtube | Playfab + Unity - Setup, Sign Up & Sign In - Jared Brandjes](https://www.youtube.com/watch?v=__M9AoiVA9c&t=1489s)

* [Youtube | Unity Store Data in Playfab | Interacting with data in PlayFab - Skye Games](https://www.youtube.com/watch?v=KoWpVuta_nE)

* [Youtube | Playlist das Aulas de SRJ 2024/25 - Diogo Andrade](https://www.youtube.com/playlist?list=PLheBz0T_uVP3JaTA4wMs38MgOKiIpDpLG)

* [Moodle | Project MPWyzards usado em Aula - Diogo Andrade](https://moodle.ensinolusofona.pt/mod/resource/view.php?id=441486)

* [Chat GPT | Diversas perguntas sobre como estruturar/resolver problemas no meu código](https://chatgpt.com/share/685f3d20-ef80-8007-ac4e-bbbb51a932e6)

## Agradecimentos

* **Mafalda Pinto -** Ajuda no conceito e design do jogo (personagens, ataques, etc...)

## Referências

[^1]: [Damage Calculation : Generation I - Bulbapedia](https://bulbapedia.bulbagarden.net/wiki/Generation_I)
