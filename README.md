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

Depois de ver alguns tutorias, comecei a fazer a implementação do meu sistema de gestão de contas. Criei um *script* chamado `AccountManager` e escrevi os seguintes métodos utilizando a SDK do **Playfab** para o **Unity** :

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

Com isto em mente, e ajuda de um tutorial, escrevi os seguintes métodos no *script* `AccountManager` :

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

Para a implementação de batalhas online privadas tive duas implementações, uma utilizando apenas o **Netcode for GameObjects** com conexões via LAN, e outra com o **Netcode  for GameObjects** + **Relay** que já permitia conexões online e sem problemas com a *firewall*. Para o projecto final só usei a segunda abordagem, mas vou falar das duas de forma a expor todo o processo do desenvolvimento do jogo e porque apenas troquei para a abordagem com o **Relay** mais tarde no projecto.

Para primeira implementação, que funcionava apenas por LAN, comecei por procurar por vários tutoriais na internet sobre o **Netcode for GameObjects** e por ver a playlist das aulas da cadeira. Percebi rapidamente que criar a conexão entre os jogadores iria ser bastante fácil, então comecei o trabalho.

Para começar, importei todos os *packages* necessários do *Package Manager*, depois criei um novo `GameObject` na minha cena chamado `NetworkManager`, adicionei-lhe o componente do mesmo nome e fiz o *setup* necessário (criação do *transport*). Depois comecei por criar um novo *script* para gerir todas as conexões no jogo chamado `ConnectionManager` com os seguintes métodos :

```c#
 public string StartHosting()
{
    string adress = GetLocalIPv4();

    UnityTransport transport = NetworkManager.Singleton
                            .GetComponent<UnityTransport>();

    transport.SetConnectionData(adress, 7777);

    NetworkManager.Singleton.StartHost();

    return adress;
}

public bool StartClientLAN(string code)
{
    string adress = code;

    UnityTransport transport = NetworkManager.Singleton
                            .GetComponent<UnityTransport>();

    transport.SetConnectionData(adress, 7777);

    bool result = NetworkManager.Singleton.StartClient();

    return result;
}

// Get Local IP Adress
private string GetLocalIPv4()
{
return Dns.GetHostEntry(Dns.GetHostName())
.AddressList.First(
f => f.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
.ToString();
}
```

Como podemos ver criei três métodos :

* O método `StartHosting()`, que começa por recolher o adereço IPv4 da máquina onde o jogo está a rodar. Depois, utiliza esse IP para dar *setup* ao `UnityTransport`, utilizando o método `SetConnectionData(int address, ulong port)`. No final simplesmente pede ao `NetworkManager`para começar o *hosting*;

* O método `StartClientLAN()`, que recebe um *join code*, este é utilizado para o *setup* do `UnityTransport` da mesma forma que o método anterior. No entanto, no final, este pede ao `NetworkManager` para começar um cliente, que se conectará ao *host* pré-establecido;

* O método `GetLocalIPv4()`, que apenas serve para captar o adereço IPv4 da máquina actual.

Esta solução funcionava, mas tinha os seus problemas, como por exemplo :

* A conexão era apenas realizada em LAN, ou seja, jogadores que não estão conectados à mesma rede wifi não podem jogar juntos;

* Este tipo de conexão pode causar problemas com a *firewall* e normalmente apareçe sempre um aviso do Windows em relação a isso.

\
Devido a estes problemas decidi então mudar para utilizar a abordagem com o **Relay** do **Unity**. Vendo alguns alguns tutoriais percebi que esta também seria bastante simples, então comecei por modificar os métodos anteriores (e por apagar o `GetLocalIPv4()`, já que este já não seria necessário) e acrescentar um novo. Acabei com os seguintes métodos no *script* `ConnectionManager :

```c#
private async Task InitializeUnityServices()
{
    try
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        Debug.Log($"Successful Login for Player {AuthenticationService.Instance.PlayerId}");
    }
    catch (Exception e)
    {
        Debug.Log($"Account Sign In Error : {e}");
    }
}

public async Task<string> StartPrivateHosting()
{
    LoadingScreenActivator.Instance.ToogleScreen(true); // UI code

    await InitializeUnityServices();

    // Get Relay Allocation
    Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);

    UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

    // Create RelayServerData
    RelayServerData relayData = AllocationUtils.ToRelayServerData(allocation, "dtls");

    // Set UnityTransport Server Data
    transport.SetRelayServerData(relayData);

    // Get the Join Code
    string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

    LoadingScreenActivator.Instance.ToogleScreen(false);

    // If the Server started, return join code, if not, return null
    return NetworkManager.Singleton.StartHost() ? joinCode : null;
}

// Client code
public async Task<bool> StartPrivateClient(string joinCode)
{
    await InitializeUnityServices();

    // Get join allocation from Relay
    JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

    UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

    // Create RelayServerData
    RelayServerData relayData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");

    // Set UnityTransport Server Data
    transport.SetRelayServerData(relayData);

    // Return the result of the StartClient method
    return NetworkManager.Singleton.StartClient();
}
```

Como podemos ver, agora tenho três métodos :

* O método `InitializeUnityServices()`, que inicializa os serviços do **Unity** e autentica uma conta anónima no **AuthenticationService** (neste caso como já faço o tratamento de contas através do **Playfab** não preciso de ter contas específicas do **Unity**), isto é importante para o **Unity** conseguir gerir as conexões online através de outros dos seus serviços;

* O método `StartPrivateHosting()`, que começa por inicializar os serviços do **Unity** com o método `InitializeUnityServices()`. Depois pede ao serviço de **Relay** do **Unity** por uma *allocation* para dois jogadores se conectarem. Quando essa *allocation* for recebida, cria um `RelayServerData`, que vai guardar as informações sobre o servidor que estamos a criar, para isto é dada a *allocation* e o tipo de conexão que é pretendido (neste caso usei o dtls porque me parecia ser o mais apropriado segundo todos os tutoriais que vi). Estes dados são depois passados ao `UnityTransport` através do método `SetRelayServerData()`, de forma a configurar a conexão que vai ser feita. Depois de ter o *transport* configurado, pede ao serviço de **Relay** para fornecer um *join code* para a *allocation* recebida. Por fim, é chamado o `NetworkManager` a quem pede que começe um *host*, se este for iniciado com sucesso, o método devolve o *join code* recebido, se não, devolve *null*;

* O método `StartPrivateClient()`, que faz, na maior parte, o mesmo processo que o `StartPrivateHosting()`, mas ao invés de pedir uma `allocation` nova ao serviço de **Relay**, usa um *join code*, que recebe como parâmetro, e pede para se juntar à *allocation* com o mesmo código. Depois disso o funcionamento do método é igual, até ao final, onde invés de começar um *host*, pede ao `NetworkManager` para começar um *client*.

É de notar que todos estes métodos utilizam o que se chama *asynchronous programming* (visto na utilização das *keywords* *async* e *await*), isto foi algo novo para mim, mas não me pareceu muito difícil. Pelo que entendi, métodos *async* funcionam mais ou menos como *threads*, permitindo que um programa corra duas tarefas ('Task') ao mesmo tempo (Atenção : Mesmo sendo parecido, um método *async* não cria *threads* novas, ou seja, não é usado *multithreading*), isto permite chamá-los de forma a não pausar o programa quando queremos realizar uma tarefa que demore tempo, bastante comum em programas que necessitam de conexões e comunicações online. Quando queremos que um método *async* espere pela execução de algo, utilizamos a *keyword* *await*, isto apenas pausa a execução desse método, não afectando o resto do programa.

Para a implementação da conexão através do **Relay**, tive alguns problemas. Estes vieram totalmente da falta de documentação atualizada da parte do **Unity**. Como estava a utilizar a nova **Multiplayer SDK**, que centraliza todos os serviços do **Unity** (antes separados em vários packages), aconteceram várias situações onde código utilizado nos tutorias/discussões que estava a acompanhar, não funcionava, pois a nova SDK mudou algumas das funcionalidades. No entanto, no final consegui resolver tudo sem grandes problemas.

#### Batalhas Públicas

A implementação de batalhas públicas com *matchmaking* foi o passo mais complicado de todo o projecto. Tentei duas implementações diferentes, das quais só a segunda funcionou.

Na primeira tentativa tentei usar os serviços **Multiplay Hosting** e **Matchmaker** do **Unity**, que me permitiam criar servidores dedicados para o meu projecto que seriam depois alocados através do serviço **Matchmaker**. No entanto esta abordagem provou-se mais difícil do que o esperado, e também mais difícil de testar, sendo que para poder testar tinha de fazer uma build de servidor em **Linux**, fazer upload para o **Multiplay**, e só aí podia testar tudo. Isto revelou-se um processo muito demorado visto que o computador onde estava a realizar o projecto é um *laptop* sem muita capacidade e que a minha internet não é a melhor. Por estes motivos decidi abandonar esta abordagem, pois para além de não estar a conseguir fazê-la funcionar, também não estava a conseguir fazer *debug* dos problemas que ocorriam de forma rápida e prática.

Na segunda tentativa, a que ficou implementada no projecto final, decidi tentar uma recentemente criada abordagem do **Unity** que usava os serviços de **Relay** e **Matchmaker**. Esta abordagem foi mais fácil de implementar e dar *debbug*, visto que usando o **Relay** tinha acesso tanto aos *logs* vindos do *host* como aos vindos dos *clients*, mas devido a ser uma coisa bastante recente no **Unity** a documentação sobre a mesma era absolutamente imprestável e também não existia nenhum conteúdo no **Youtube** sobre. Para  conseguir pô-la a funcionar tive de procurar pelo forum **Unity Discussions** por pessoas que também estavam a tentar fazer o mesmo que eu. Foi por aí que consegui no final ter um sistema funcional que conectasse jogadores baseado em *matchmaking*. Visto que esta foi a única abordagem que funcionou, apenas vou entrar em detalhe sobre ela neste relatório.

Passando agora a explicar mais detalhadamente a implementação da abordagem com o **Relay + Matchmaker**, decidi utilizar na mesma o meu *script* `ConnectionManager`, e acresentei um novo método :

```c#
public async void FindMatch()
{
    if (!AccountManager.Instance.IsLoggedIn) return;

    Debug.Log("Looking for Match");

    LoadingScreenActivator.Instance.ToogleScreen(true);

    await InitializeUnityServices();

    _matchConnection = true;

    try
    {
        Debug.Log($"[Network] [Matchmaker Manager] Find Match!");

        // Create matchmaker options to specify the correct Queue to use and the player parameters
        var matchmakerOptions = new MatchmakerOptions
        {
            QueueName = "PlayerEXP",
            PlayerProperties = new Dictionary<string, PlayerProperty>() {
            { "LV", new PlayerProperty(_localPlayer.Player.Level.ToString())}
        }
        };

        // Create session options to specify number of players and Relay usage
        var sessionOptions = new SessionOptions() { MaxPlayers = 2 }.WithRelayNetwork();

        // Create a cancellation source for the Session 
        var cancellationSource = new CancellationTokenSource();

        // Ask the Multiplayer Services to Matchmake a Session based on the previous parameters
        _matchedSession = await MultiplayerService.Instance.MatchmakeSessionAsync(matchmakerOptions, sessionOptions, cancellationSource.Token);

        // Store the results of the Matchmaked Session
        StoredMatchmakingResults matchmakingResults = await MatchmakerService.Instance.GetMatchmakingResultsAsync(_matchedSession.Id);

        Debug.Log($"[Network] [Matchmaker Manager] Matchmaking results: {matchmakingResults}");

    }
    catch (Exception e)
    {
        Debug.Log($"[Network] [Matchmaker Manager] Matchmaking failed : {e}!");
    }
    finally
    {
        LoadingScreenActivator.Instance.ToogleScreen(false);
    }
}
```

Como podemos ver só criei um método :

* O método `FindMatch()`, que começa por inicializar os serviços do **Unity**, assim como os métodos das batalhas privadas. Depois tenta criar um `MatchMakerOptions`, que guarda a informação relacionada ao serviço **Matchmaker** (*Queue* a ser usada, propriedades do jogador importantes para o pareamento), um `SessionOptions`, que guarda informações sobre a sessão de jogo a ser criada (número de jogadores) e é definida como sendo uma sessão de **Relay**, e um `CancellationTokenSource`, que nos deixa parar a sessão criada quando for necessário. Estes três objectos são depois usados para tentar pedir ao serviço de `Multiplayer` para começar uma sessão com **matchmaking** chamando o método `MatchmakeSessionAsync()`. Depois disto o método chama o serviço **Matchmaker** e pede-lhe para obter os resultados da sessão criada, passando o seu *id* como parâmetro. Esta operação esta encapsulada num bloco *try catch*, de froma a controlar os erros que possam acontecer, caso aconteçem é apenas imprimida uma mensagem na consola do **Unity**, ou no caso de uma *build*, esta fica registada no ficheiro *Player.log*.

Para informações mais específicas em relação a como funciona o *matchmaking*, refir à secção **Matchmaking**.

#### Sincronização das Batalhas

Para fazer a sincronização das batalhas, de forma a permitir que 2 jogadores conseguissem jogar um contra o outro, comecei por pesquisar por vários exemplos em tutorias no **Youtube** e também no projecto dado nas aulas.

Para o funcionamento das batalhas tenho 3 *scripts* essenciais, o `ConnectionManager`, que está sempre em cena e é responsável tanto pelas conexões entre os jogadores, como por agregar todas as informações necessárias para começar uma partida (e a começar), o `PlayerNetwork`, que é parte do *Player Prefab* (definido no meu `NetworkManager`) e é responsável por guardar e passar os dados de cada jogador através da *network*, e o `BattleManager`, que faz parte de um prefab spawnado na *network* e é responsável por gerir o funcionamento de toda a batalha do inicío ao fim.

No *script* `ConnectionManager` fiz isto :

```c#
private BattleManager _serverBattle;
private List<PlayerData> _players;

private void Start()
{
    _players = new List<PlayerData>();
}

private void Update()
{
    if (_serverOn)
    {
        Debug.Log("SERVER IS ON");
        if (IsServer)
        {
            _numberOfClients = NetworkManager.Singleton.ConnectedClientsList.Count;
            Debug.Log($"Players Connected : {_numberOfClients}/2");
        }

        // If we are in a matched connection, there is no UI for starting a battle
        // so we need to start it automatically once we have 2 players conected

        if (IsHost && _matchConnection)
        {
            if (_numberOfClients == 2) StartBattle();
            _matchConnection = false;
        }
    }
}

public bool StartBattle()
{
    if (_numberOfClients != 2) return false;

    GetPlayerData();

    GameObject battle = Instantiate(_battleScreen);
    NetworkObject netBattle = battle.GetComponent<NetworkObject>();

    _serverBattle = battle.GetComponent<BattleManager>();

    _serverBattle.AddPlayers(_players);

    netBattle.Spawn();

    ToogleMainMenuClientRpc(false);
    return true;
}

private void GetPlayerData()
{
    _players.Clear();

    foreach (ulong clientId in NetworkManager.Singleton.ConnectedClients.Keys)
    {
        var playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;

        PlayerData pd = playerObject.GetComponent<PlayerNetwork>().Player;

        _players.Add(pd);
    }
}
```

Como podemos ver criei 2 variáveis e 2 métodos importantes :

* A variável `_serverBattle` do tipo `BattleManager`, que guarda uma instância do componente `BattleManager`;

* A variável `_players` do tipo `List<PlayerData>`, que guarda uma lista de informações sobre os dois jogadores conectados, esta depois será usada para fazer o *setup* das batalhas;

* O método `StartBattle()`, que começa por verificar se já estão conectados dois jogadores, se não estiverem, avaba a sua execução aqui, se estiverem, continua a execução. Depois dessa verificação chama o método `GetPlayerData()` e instancia uma cópia no *prefab* das batalhas (que contêm o *script* `BattleManager`) recolhendo também uma referencia ao seu componente `NetworkObject` e  `BattleManager`. Depois de já ter recolhido as referencias, o método passa a lista `_players` ao `BattleManager` e faz *spawn* do *prefab* da batalha na *network*, ou seja, em todos os *clients*;

* O método `GetPlayerData()`, que utiliza o `NetworkManager` para recolher os *PlayerObjects* de todos os *clients*, e recolher `PlayerData` de cada jogador guardada no componente `PlayerNetwork`.

No *script* `PlayerNetwork` fiz isto :

```c#
public class PlayerNetwork : NetworkBehaviour
{
    private NetworkVariable<PlayerData> _player = new NetworkVariable<PlayerData>(default,
                        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public PlayerData Player => _player.Value;
    private PlayerController _ctrl;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        _ctrl = FindAnyObjectByType<PlayerController>();
        SetPlayer();
        Debug.Log("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
    }

    public void SetPlayer()
    {
        if (!IsOwner) return;

        Player p = _ctrl.Player;
        Debug.Log($"Player : {p.Name}");

        string[] moves = p.Creature.CurrentAttackSet.ConvertAll(
                                                    (m) => m.name
                                                    .Replace("(Clone)", ""))
                                                    .ToArray();

        FixedString32Bytes[] movesF = new FixedString32Bytes[4];

        for (int i = 0; i < moves.Length; i++) movesF[i] = moves[i];

        ulong id = NetworkManager.Singleton.LocalClientId;

        _player.Value = new PlayerData(p.Name, p.EXP, id, p.Creature.Name, movesF);

        Debug.Log(p.Name);
        Debug.Log(p.EXP);
        Debug.Log(moves[0]);
    }
}
```

Como podemos ver criei 2 variáveis e 1 método importantes :

* Uma `NetworkVariable` (variável sincronizada dentro de toda a *network*) `_player` do tipo `PlayerData`, que guarda todos os dados importantes sobre o jogador ligado a este *prefab*;

* A variável `_ctrl` do tipo `PlayerController`, que guarda uma referência ao jogador original do `PlayerNetwork`, esta variável só é guardada no jogo do próprio jogador (e não sincronizada na *network*) o seu unico propósito é contruir a `PlayerData`;

* O método `SetPlayer()`, que começa por ir buscar ao `_ctrl` uma referência ao seu `Player` para retirar toda a informação relevante dele. Depois de esta informação ser obtida, cria uma nova instância da *struct* `PlayerData` com toda essa informação.

A *struct* `PlayerData` mencionada acima contêm os seguintes membros :

```c#
public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
{
    public string Name => _name.ToString();
    public int EXP => _exp;
    public ulong ID => _id;
    public string Creature => _creature.ToString();
    public string Move1 => _move1.ToString();
    public string Move2 => _move2.ToString();
    public string Move3 => _move3.ToString();
    public string Move4 => _move4.ToString();
    private FixedString32Bytes _name;
    private int _exp;
    private ulong _id;
    private FixedString32Bytes _creature;
    private FixedString32Bytes _move1;
    private FixedString32Bytes _move2;
    private FixedString32Bytes _move3;
    private FixedString32Bytes _move4;

    public PlayerData(FixedString32Bytes name, int exp, ulong id, FixedString32Bytes creature, params FixedString32Bytes[] moves)
    {
        _name = name;
        _exp = exp;
        _id = id;
        _creature = creature;
        _move1 = moves[0];
        _move2 = moves[1];
        _move3 = moves[2];
        _move4 = moves[3];
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref _name);
        serializer.SerializeValue(ref _exp);
        serializer.SerializeValue(ref _id);
        serializer.SerializeValue(ref _creature);
        serializer.SerializeValue(ref _move1);
        serializer.SerializeValue(ref _move2);
        serializer.SerializeValue(ref _move3);
        serializer.SerializeValue(ref _move4);
    }

    public bool Equals(PlayerData other)
    {
        return Name.Equals(other.Name)
            && EXP == other.EXP
            && ID == other.ID
            && Creature.Equals(other.Creature)
            && Move1.Equals(other.Move1)
            && Move2.Equals(other.Move2)
            && Move3.Equals(other.Move3)
            && Move4.Equals(other.Move4);
    }
}
```

No *script* `BattleManager` fiz isto :

```c
public class BattleManager : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();


        if (_playerDataAdd.Count > 0)
        {
            foreach (PlayerData p in _playerDataAdd) AddPlayerDatasClientRpc(p);

            SetPlayersClientRpc();
        }
        else
        {
            Debug.LogWarning("PLAYERS DATA NOT SYNCHORINZED");
        }
    }

    private void SetUp()
    {
        if (!IsServer) return;

        Debug.LogWarning("BBBBBBBB");

        _playerActions = new List<string>();

        OnTurnPassed += () => Debug.Log($"TURN {Turn}");

        _wfd = new WaitForDialogueEnd(_dialogueManager);

        Debug.LogWarning($"PLAYER 1 ID : {_players[0].ID}");
        Debug.LogWarning($"PLAYER 2 ID : {_players[1].ID}");

        InitializeUIClientRpc();

        Debug.LogWarning("BBBBBBBB");
        StartCoroutine(BattleStart());
    }

    [ClientRpc]
    private void InitializeUIClientRpc()
    {
        foreach (Player p in _players)
        {
            if (p.ID == NetworkManager.Singleton.LocalClientId)
            {
                _dialogueManager.SetUpDialogueManager();
                _ui.SetUpUI(p);
                break;
            }
        }
    }

    public void AddPlayers(List<PlayerData> players)
    {
        if (players.Count == 2)
            foreach (PlayerData p in players) _playerDataAdd.Add(p);

    }
    [ClientRpc]
    private void AddPlayerDatasClientRpc(PlayerData p)
    {
        _playerDatas.Add(p);
    }

    [ClientRpc]
    private void SetPlayersClientRpc()
    {
        _players.Clear();

        for (int i = 0; i < 2; i++)
        {
            int icapture = i;

            Player newP = _playerBase.CreatePlayer(_playerDatas[i].Name);

            newP.SetEXP(_playerDatas[i].EXP);
            newP.SetId(_playerDatas[i].ID);
            newP.LoadCreature(_playerDatas[i].Creature, _playerDatas[i].Move1,
                                                        _playerDatas[i].Move2,
                                                        _playerDatas[i].Move3,
                                                        _playerDatas[i].Move4);

            _players.Add(newP);
            Debug.Log($"Add player {newP.Name} in {NetworkManager.Singleton.LocalClientId}, idP {newP.ID}");

            OnTurnPassed += () => _players[icapture].Creature.SetTurn(Turn);
            OnTurnPassed += () => _players[icapture].Creature.CheckModifier();
        }
        for (int i = 0; i < 2; i++)
        {
            int otherIdx = i == 0 ? 1 : 0;
            _players[i].Creature.SetOpponent(_players[otherIdx].Creature);
        }

        SetUp();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RegisterActionServerRpc(string creature, int attackId, int speed)
    {
        Debug.Log($"ACTION REGISTRED FOR {creature}");

        string attack = creature + "|" + attackId + "|" + speed;

        _playerActions.Add(attack);
    }
    private Attack GetAction(string creature, int attack)
    {
        foreach (Player p in _players)
        {
            if (p.Creature.Name == creature)
            {
                Debug.Log($"{creature}, long {p.Creature.CurrentAttackSet.Count}, id{attack}");
                return p.Creature.CurrentAttackSet[attack];
            }
        }
        return null;
    }

    [ClientRpc]
    private void DoAttackClientRpc(string attackString)
    {
        string[] attackInfo = attackString.Split("|");

        Debug.Log($"{attackInfo[0]} DOING ATTACK {attackInfo[1]}");

        Attack attack = GetAction(attackInfo[0], int.Parse(attackInfo[1]));

        (float damage, float recoil) = attack.DoAttack();

        Debug.Log(damage);

        if (attack.Target.CurrentHP - damage <= 0)
        {
            _hasWinner = true;
            _winnerID = attack.Attacker.Owner.ID;
            _winnerName = attack.Attacker.Owner.Name;
        }

        if (damage > 0)
        {
            StartCoroutine(attack.Target.ApplyDamage(damage));

            if (recoil > 0) StartCoroutine(attack.Attacker.ApplyDamage(recoil));
        }

        Debug.Log($"CHECK OPPONENT HP : {attack.Target.CurrentHP}");
    }

    [ClientRpc]
    private void UpdateDialogueClientRpc()
    {
        _dialogueManager.StartDialogues();
    }

    [ClientRpc]
    private void ClearDialogueClientRpc()
    {
        _dialogueManager.ClearDialogues();
    }

    [ClientRpc]
    private void UpdateUIClientRpc()
    {
        _ui.SetUpActionScene();
    }

    public IEnumerator BattleStart()
    {
        Debug.Log("START BATTLE COROUTINE");

        while (!_hasWinner)
        {
            UpdateUIClientRpc();

            Debug.Log($"HAS WINNER : {_hasWinner}");
            Turn++;

            yield return new WaitForPlayerActions(() => _playerActions.Count == _actionsListSize);
            yield return new WaitForSeconds(1);

            OrganizeActions();

            yield return new WaitForEndOfFrame();

            DoAttackClientRpc(_playerActions[0]);

            UpdateDialogueClientRpc();

            yield return new WaitForEndOfFrame();
            yield return _wfd;

            DoAttackClientRpc(_playerActions[1]);

            UpdateDialogueClientRpc();

            yield return new WaitForEndOfFrame();
            yield return _wfd;

            _playerActions.Clear();
            ClearDialogueClientRpc(); // Clear Dialogues list for all clients to prevent de-synchronization in the texts
        }

        FinnishBattleClientRpc(_winnerID, _winnerName);
    }
    private void OrganizeActions()
    {
        if (int.Parse(_playerActions[0].Split("|")[2]) < int.Parse(_playerActions[1].Split("|")[2]))
        {
            string tmp = _playerActions[0];

            _playerActions[0] = _playerActions[1];
            _playerActions[1] = tmp;
        }
    }

    [ClientRpc]
    private void FinnishBattleClientRpc(ulong winnerID, string winnerName)
    {
        _dialogueManager.StartDialogues($"{winnerName} WON!");

        if (winnerID == NetworkManager.Singleton.LocalClientId)
        {
            PlayerController p = FindAnyObjectByType<PlayerController>(0);
            p.Player.SetEXP(p.Player.EXP + 50);
            AccountManager.Instance.SavePlayerData(
                new Dictionary<string, string>()
                {
                    { "EXP", p.Player.EXP.ToString() }
                }
            );
        }

        StartCoroutine(NerworkShutdown());
    }
    private IEnumerator NerworkShutdown()
    {
        yield return new WaitForSeconds(2);

        ConnectionManager.Instance.ToogleMainMenu(true);
        NetworkManager.Singleton.Shutdown();
    }

}
```

Como podemos ver criei vários métodos importantes, dos quais :

* O método `AddPlayers()` e o método `AddPlayerDatasClientRPC()`, que tratam de sincronizar os dados relativos á lista de `PlayerData` em todos os *clients*. Sendo que o primeiro método recebe os dados e guarda-os na cópia do jogo que está a fazer *hosting* e o segundo tranfere-os para outra lista, sincronizando os dados da mesma nos *clients* por ser um RPC (que é efectuado em todos os *clients*);

* O método `SetPlayersClientRPC()`, que cria em cada *client* instâncias de `Player`com as informações dos jogadores que estão na batalha. Este método também dá a cada `Player` o *ID* do seu *client*, de modo a poder identificar mais tarde quem é o seu *owner*;

* O método `SetUp()`, só executado no servidor, que faz o *setup* de várias variáveis, inicializa o UI em todos os *clients* e no fim começa a corrotina que gere o *loop* da batalha;

* O método `InitializeUIClientRPC()`, que por cada *client* faz o *setup* da UI baseada no seu `Player`, isto é feito comparando o *ID* do `Player` com o *ID* do *client* local (sabido usando o `NetworkManager`) e só executando o método de *setup* do UI quando estes corresponderem;

* O método `BattleStart()`, só executado no servidor, que é uma corrotina que gere o funcionamento do *loop* de jogo de uma batalha. Este usa da variável `_playerActions` para saber quando todos os jogadores selecionaram um ataque, depois organiza-as utilizando o método `OrganizeActions()`. Apartir daí a corrotina utiliza de vários RPCs para efectuar os ataques dos jogadores e atualizar a UI em todos os *clients*. Quando a corrotina detecta que um jogador venceu, através da variável `_hasWinner`, para a execução do *loop* principal e chama o método `FinnishBattleClientRPC()`;

* O método `RegisterActionServerRPC()` e o método `GetAction()`, que tratam de registar no servidor as ações de cada jogador. Sendo que o primeiro é subscrito em botões de UI em cada *client*, passando informações serializadas (numa *string*) dos ataques que são pretendidos, e o segundo é reponsável por traduzir a informação serializada para uma instância de `Attack`;

* O método `DoAttackClientRPC()`, que recebe informações sobre o ataque a realizar e as converte numa instância de `Attack` através do método `GetAction()`, depois realiza o ataque pretendido, extraindo o dano feito e o dano de *recoil*. Com estes valores, primeiro verifica se a criatura atacada vai morrer, se sim muda o valor das variáveis `_hasWinner`, `_winnerID`e `_winnerName` de acordo com quem ganhou, e depois aplica o dano normal e de recoil a cada criatura;

* O método `OrganizeActions()`, que recebe a lista de informações serializadas dos ataques dos jogadores, e a reorganiza de acordo com o valor da sua *speed*;

* O método `FinnishBattleClientRPC()`e o método `NetworkShutdown()`, que tratam de acabar a batalha e desconectar os *clients* da partida. Sendo que o primeiro trata de mostrar no UI o nome do vencedor e de dar ao devido os seus pontos de EXP, e o segundo de desconectar os *clients* da sessão com um pequeno *delay*.

* Os vários métodos de atualização de UI como `UpdateDialogueClientRPC()`, `UpdateUIClientRPC()` e `ClearDialogueClientRPC()`, que apenas servem para fazer mudanças no UI de todos os *clients* em simultâneo.

É de notar que muitos destes métodos são RPCs (Remote Procedure Calls) que basicamente nos deixam ter um método que pode ser chamado pelo servidor, e executado em todos os *clients* (*ClientRPCs*), ou por um *client* e executado no servidor. Isto é útil quando queremos fazer sincronização de eventos ou variáveis ou quando queremos que o servidor tenha de dar permissão aos *clients* para poderem fazer algo.

### Diagrama de Arquitetura Redes

\
![a](Images/DiagramRedes.png)

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

## Análise de Bandwidth

Depois de realizar alguns jogos, analizei os dados do meu projecto na **Unity Cloud**, mais especificamente os dados relacionados ao uso do **Relay**.

![a](Images/TotalBandwidth.png)

![a](Images/AverageBytes.png)

Observando os gráficos vi que, por 1 hora de realização de batalhas (não consecutivas), foram consumidos no total 157,3 KiB (161.075,2 bytes), e por pico de KiB mostrado no gráfico não se chegaram a enviar mais de 27 KiB (27.648 bytes). Tendo em conta que durante esta hora foram hosteadas 24 alocações (algumas delas que não chegaram a fazer uma partida completa, pelo menos 5 delas foram só testes de conexão), o uso médio em KiB é estimado como ~10 KiB por batalha.

Conseguimos também ver que o número de KiB em média por usuários simultâneos (CCU) é de 61.5 KiB/CCU.

Nenhum destes valores chega perto de atingir o valor máximo dado pelo *tier* grátis do serviço de **Relay** do **Unity**. Assim acabei por concluir que o jogo podia ficar de pé vários meses (tendo em conta que também não estou à espera de uma afluência gigante de jogadores) apenas utilizando os serviços grátis do **Unity** sem problemas.

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
Existem problemas na sincronização da UI (sendo que o jogador que serve de *Host* tem controlo máximo sobre quando o UI é mudado, causando alguma desincronização quando os jogadores passam o texto em tempos diferentes).

\
Ás vezes existem problemas nos cálculos do dano, podendo haver divergências entre *clients*.

## Bibliografia

* [Unity Discussions | Get the device IP address from Unity](https://discussions.unity.com/t/get-the-device-ip-address-from-unity/235351/2)

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
