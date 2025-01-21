using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Aws.GameLift.Realtime.Event;
using Aws.GameLift.Realtime;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.CognitoIdentity;
using Amazon;
using System.Text;
using System.Net;
using System.Net.Sockets;
using UnityEngine.InputSystem.XR;
using System.Threading.Tasks;


namespace MoonshineStudios.CharacterInputController {
    public class serverConnect : MonoBehaviour
    {
        [Header("Controllers")]

        public gameController gameController;
        public uiController uiController;
        public bool IsConnectedToServer { get; set; }

        private Client _client;
        private Queue<Action> _mainThreadQueue = new Queue<Action>();

        private const int CURRENT_PLAYER_ACCEPTED = 100;
        private const int PLAYER_ACCEPTED = 101;
        private const int PLAYER_DISCONNECTED = 102;
        private const int CURRENT_PLAYER_UPDATE = 200;
        private const int PLAYER_UPDATE = 201;
        private const int GAME_OVER = 203;

        [Serializable]
        public class GameProperty
        {
            public string Key;
            public string Value;
        }

        [Serializable]
        public class PlayerSessionObject
        {
            public string PlayerSessionId;
            public string GameSessionId;
            public string FleetId;
            public string IpAddress;
            public string Port;
            public string PlayerId;
            public string PlayerData;
            public string GameSessionName;
            public string CreationTime;
            public string Status;
            public string TerminationTime;
            public GameProperty[] GameProperties;
        }
        public class CreateGameParameters
        {
            public bool createNew;
            public int maxPlayers;
            public int numArtifacts;
        }


        // Start is called before the first frame update
        void Start()
        {
            IsConnectedToServer = false;
        }

        // Update is called once per frame
        void Update()
        {
            RunMainThreadQueueActions();
        }

        public void ConnectToGameLiftServer(bool createNew = false, int maxPlayers = 2, int numArtifacts = 3)
        {
            StartCoroutine(ConnectToGameLiftServerCoroutine(createNew, maxPlayers, numArtifacts));
        }

        private IEnumerator ConnectToGameLiftServerCoroutine(bool createNew, int maxPlayers, int numArtifacts)
        {
            Debug.Log("Reaching out to client service Lambda function");

            // Add request logging
            var gameParams = new CreateGameParameters
            {
                createNew = createNew,
                maxPlayers = maxPlayers,
                numArtifacts = numArtifacts
            };

            Debug.Log($"Sending request with parameters: createNew={createNew}, maxPlayers={maxPlayers}, numArtifacts={numArtifacts}");

            CognitoAWSCredentials credentials = null;
            AmazonLambdaClient client = null;
            InvokeRequest request = null;

            try
            {
                credentials = new CognitoAWSCredentials(
                    Utils.identityPoolId,
                    RegionEndpoint.USEast1
                );

                client = new AmazonLambdaClient(credentials, RegionEndpoint.USEast1);

                request = new InvokeRequest
                {
                    FunctionName = Utils.lambdaFunction,
                    InvocationType = InvocationType.RequestResponse,
                    Payload = JsonUtility.ToJson(gameParams)
                };

                Debug.Log($"Lambda request payload: {request.Payload}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error setting up Lambda request: {e}");
                uiController.ShowPopupError();
                yield break;
            }

            var taskCompletionSource = new TaskCompletionSource<InvokeResponse>();

            try
            {
                client.InvokeAsync(request)
                    .ContinueWith(task => {
                        if (task.IsFaulted)
                        {
                            Debug.LogError($"Lambda invoke faulted: {task.Exception}");
                            taskCompletionSource.SetException(task.Exception);
                        }
                        else if (task.IsCanceled)
                        {
                            Debug.LogError("Lambda invoke cancelled");
                            taskCompletionSource.SetCanceled();
                        }
                        else
                        {
                            var result = task.Result;
                            // Log the raw response
                            if (result.Payload != null)
                            {
                                var rawResponse = System.Text.Encoding.UTF8.GetString(result.Payload.ToArray());
                                Debug.Log($"Raw Lambda response: {rawResponse}");
                            }
                            taskCompletionSource.SetResult(result);
                        }
                    });
            }
            catch (Exception e)
            {
                Debug.LogError($"Error invoking Lambda: {e}");
                uiController.ShowPopupError();
                yield break;
            }

            float timeout = 30f;
            float elapsed = 0f;

            while (!taskCompletionSource.Task.IsCompleted)
            {
                if (elapsed >= timeout)
                {
                    Debug.LogError("Lambda connection timeout");
                    uiController.ShowPopupError();
                    yield break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            InvokeResponse response = null;
            try
            {
                response = taskCompletionSource.Task.Result;

                // Log full response details
                Debug.Log($"Lambda response - StatusCode: {response.StatusCode}, FunctionError: {response.FunctionError}");
                if (response.Payload != null)
                {
                    var responsePayload = System.Text.Encoding.UTF8.GetString(response.Payload.ToArray());
                    Debug.Log($"Response payload: {responsePayload}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting Lambda response: {e}");
                uiController.ShowPopupError();
                yield break;
            }

            if (!string.IsNullOrEmpty(response.FunctionError))
            {
                Debug.LogError($"Lambda function error: {response.FunctionError}");
                Debug.LogError($"Error payload: {System.Text.Encoding.UTF8.GetString(response.Payload.ToArray())}");
                uiController.ShowPopupError();
                yield break;
            }

            if (response.StatusCode != 200)
            {
                Debug.LogError($"Wrong status code: {response.StatusCode}");
                uiController.ShowPopupError();
                yield break;
            }

            try
            {
                var payload = System.Text.Encoding.UTF8.GetString(response.Payload.ToArray());
                Debug.Log($"Processing payload: {payload}");

                var playerSessionObj = JsonUtility.FromJson<PlayerSessionObject>(payload);

                if (playerSessionObj == null)
                {
                    Debug.LogError("Failed to deserialize player session object");
                    uiController.ShowPopupError();
                    yield break;
                }

                if (string.IsNullOrEmpty(playerSessionObj.FleetId))
                {
                    Debug.LogError($"Error in Lambda: {payload}");
                    uiController.ShowPopupError();
                    yield break;
                }

                Debug.Log($"Successfully processed player session. Fleet ID: {playerSessionObj.FleetId}");

                if (playerSessionObj.GameProperties != null)
                {
                    foreach (var prop in playerSessionObj.GameProperties)
                    {
                        if (prop.Key == "numArtifacts")
                        {
                            gameController.StoreArtifactCount(int.Parse(prop.Value));
                        }
                    }
                }

                Debug.Log($"Connecting to server at {playerSessionObj.IpAddress}:{playerSessionObj.Port}");
                ActionConnectToServer(
                    playerSessionObj.IpAddress,
                    Int32.Parse(playerSessionObj.Port),
                    playerSessionObj.PlayerSessionId);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing Lambda response: {e}");
                uiController.ShowPopupError();
                yield break;
            }
        }

        public void ActionConnectToServer(string ipAddr, int port, string tokenUID)
        {
            StartCoroutine(ConnectToServer(ipAddr, port, tokenUID));
        }

        // common code whether we are connecting to a GameLift hosted server or
        // a local server
        private IEnumerator ConnectToServer(string ipAddr, int port, string tokenUID)
        {
            ConnectionToken token = new ConnectionToken(tokenUID, null);

            ClientConfiguration clientConfiguration = ClientConfiguration.Default();

            _client = new Client(clientConfiguration);
            _client.ConnectionOpen += new EventHandler(OnOpenEvent);
            _client.ConnectionClose += new EventHandler(OnCloseEvent);
            _client.DataReceived += new EventHandler<DataReceivedEventArgs>(OnDataReceived);
            _client.ConnectionError += new EventHandler<ErrorEventArgs>(OnConnectionErrorEvent);

            int UDPListenPort = FindAvailableUDPPort();
            if (UDPListenPort == -1)
            {

                uiController.ShowPopupError();
                Debug.Log("Unable to find an open UDP listen port");
                yield break;
            }
            else
            {

                Debug.Log($"UDP listening on port: {UDPListenPort}");
            }


            Debug.Log($"[client] Attempting to connect to server ip: {ipAddr} TCP port: {port} Player Session ID: {tokenUID}");
            _client.Connect(ipAddr, port, UDPListenPort, token);

            while (true)
            {
                if (_client.ConnectedAndReady)
                {
                    IsConnectedToServer = true;

                    Debug.Log("[client] Connected teo server");
                    break;
                }
                yield return null;
            }
        }

        public void DisconnectFromServer()
        {
            if (_client != null && _client.Connected)
            {
                _client.Disconnect();
            }
        }

        private void OnOpenEvent(object sender, EventArgs e)
        {
            Debug.Log("[server-sent] OnOpenEvent");
        }

        private void OnCloseEvent(object sender, EventArgs e)
        {
            Debug.Log("[server-sent] OnCloseEvent");
        }

        private void OnConnectionErrorEvent(object sender, ErrorEventArgs e)
        {
            uiController.ShowPopupError();
            Debug.Log($"[client] Connection Error! : ");
        }

        private void OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            string data = Encoding.Default.GetString(e.Data);

            switch (e.OpCode)
            {
                case CURRENT_PLAYER_ACCEPTED:
                    QForMainThread(OnCurrentPlayerAccepted, e.Sender, data);
                    break;

                case PLAYER_ACCEPTED:
                    QForMainThread(OnPlayerAccepted, e.Sender);
                    break;

                case CURRENT_PLAYER_UPDATE:
                    QForMainThread(OnCurrentPlayerChanged);
                    break;

                case PLAYER_UPDATE:
                    QForMainThread(OnPlayerChanged, JsonUtility.FromJson<PlayerData>(data));
                    break;

                case PLAYER_DISCONNECTED:
                    QForMainThread(OnPlayerDisconnected, e.Sender);
                    break;

                case GAME_OVER:
                    QForMainThread(OnGameOver, e.Sender);
                    break;
            }
        }


        public void OnGameOver(int winnerId)
        {
            gameController.HandleGameOver(winnerId);
            // Disconnect after processing game over
            DisconnectFromServer();
        }

        public void NotifyGameWon()
        {
            if (_client != null && _client.Connected)
            {
                _client.SendEvent(GAME_OVER, new byte[0]);
                // Don't disconnect immediately - wait for server to process
            }
        }

        // given a starting and ending range, finds an open UDP port to use as the listening port
        private int FindAvailableUDPPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        private void QForMainThread(Action fn)
        {
            lock (_mainThreadQueue)
            {
                _mainThreadQueue.Enqueue(() => { fn(); });
            }
        }

        private void QForMainThread<T1>(Action<T1> fn, T1 p1)
        {
            lock (_mainThreadQueue)
            {
                _mainThreadQueue.Enqueue(() => { fn(p1); });
            }
        }

        private void QForMainThread<T1, T2>(Action<T1, T2> fn, T1 p1, T2 p2)
        {
            lock (_mainThreadQueue)
            {
                _mainThreadQueue.Enqueue(() => { fn(p1, p2); });
            }
        }

        private void QForMainThread<T1, T2, T3>(Action<T1, T2, T3> fn, T1 p1, T2 p2, T3 p3)
        {
            lock (_mainThreadQueue)
            {
                _mainThreadQueue.Enqueue(() => { fn(p1, p2, p3); });
            }
        }

        private void RunMainThreadQueueActions()
        {
            // as our server messages come in on their own thread
            // we need to queue them up and run them on the main thread
            // when the methods need to interact with Unity
            lock (_mainThreadQueue)
            {
                while (_mainThreadQueue.Count > 0)
                {
                    _mainThreadQueue.Dequeue().Invoke();
                }
            }
        }

        public void OnPlayerAccepted(int playerId)
        {
            gameController.OnPlayerAccepted(playerId);
        }

        public void OnCurrentPlayerAccepted(int playerId, string data)
        {
            gameController.OnCurrentPlayerAccepted(playerId, data);
        }

        public void OnCurrentPlayerChanged()
        {
            gameController.OnCurrentPlayerChanged();
        }

        public void OnPlayerChanged(PlayerData playerData)
        {
            gameController.OnPlayerChanged(playerData);
        }

        public void OnPlayerDisconnected(int playerId)
        {
            gameController.OnPlayerDisconnected(playerId);
        }

        public void ChangePlayer(PlayerData playerData)
        {
            // inform server the hop button was pressed by local player
            _client.SendEvent(PLAYER_UPDATE, Encoding.UTF8.GetBytes(JsonUtility.ToJson(playerData)));
        }
    }
}

