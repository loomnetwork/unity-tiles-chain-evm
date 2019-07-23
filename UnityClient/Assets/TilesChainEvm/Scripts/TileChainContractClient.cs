using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Loom.Client;
using Loom.Nethereum.ABI.FunctionEncoding.Attributes;
using UnityEngine;

namespace Loom.Unity3d.Samples.TilesChainEvm
{
    /// <summary>
    /// Abstracts interaction with the contract.
    /// </summary>
    public class TileChainContractClient
    {
        private readonly byte[] privateKey;
        private readonly byte[] publicKey;
        private readonly Address contractAddress;
        private readonly ILogger logger;
        private readonly Queue<Action> eventActions = new Queue<Action>();
        private EvmContract contract;
        private DAppChainClient client;
        private IRpcClient reader;
        private IRpcClient writer;

        public event Action<JsonTileMapState> TileMapStateUpdated;

        public TileChainContractClient(byte[] privateKey, byte[] publicKey, Address address, ILogger logger)
        {
            this.privateKey = privateKey;
            this.publicKey = publicKey;
            this.contractAddress = address;
            this.logger = logger;
        }

        public bool IsConnected =>
            this.contract != null &&
            this.contract.Client.ReadClient.ConnectionState == RpcConnectionState.Connected &&
            this.contract.Client.WriteClient.ConnectionState == RpcConnectionState.Connected;

        public async Task ConnectToContract()
        {
            if (this.contract == null)
            {
                this.contract = await GetContract();
            }
        }

        public async Task<JsonTileMapState> GetTileMapState()
        {
            await ConnectToContract();

            TileMapStateOutput result = await this.contract.StaticCallDtoTypeOutputAsync<TileMapStateOutput>("GetTileMapState");
            if (result == null)
                throw new Exception("Smart contract didn't return anything!");

            JsonTileMapState jsonTileMapState = JsonUtility.FromJson<JsonTileMapState>(result.State);
            return jsonTileMapState;
        }

        public async Task SetTileMapState(JsonTileMapState jsonTileMapState)
        {
            await ConnectToContract();

            string tileMapState = JsonUtility.ToJson(jsonTileMapState);
            await this.contract.CallAsync("SetTileMapState", tileMapState);
        }

        public void Update()
        {
            while (this.eventActions.Count > 0)
            {
                Action action = this.eventActions.Dequeue();
                action();
            }
        }

        private async Task<EvmContract> GetContract()
        {
            this.writer = RpcClientFactory.Configure()
                .WithLogger(Debug.unityLogger)
                .WithWebSocket("ws://127.0.0.1:46658/websocket")
                .Create();

            this.reader = RpcClientFactory.Configure()
                .WithLogger(Debug.unityLogger)
                .WithWebSocket("ws://127.0.0.1:46658/queryws")
                .Create();

            this.client = new DAppChainClient(this.writer, this.reader)
                { Logger = this.logger };

            // required middleware
            this.client.TxMiddleware = new TxMiddleware(new ITxMiddlewareHandler[]
            {
                new NonceTxMiddleware(this.publicKey, this.client),
                new SignedTxMiddleware(this.privateKey)
            });

            await this.client.ReadClient.ConnectAsync();
            await this.client.WriteClient.ConnectAsync();

            var callerAddr = Address.FromPublicKey(this.publicKey);
            EvmContract evmContract = new EvmContract(this.client, this.contractAddress, callerAddr, GetAbi());

            evmContract.EventReceived += this.EventReceivedHandler;
            await evmContract.Client.SubscribeToAllEvents();
            return evmContract;
        }

        private void EventReceivedHandler(object sender, EvmChainEventArgs e)
        {
            if (e.EventName != "OnTileMapStateUpdate")
                return;

            OnTileMapStateUpdateEvent onTileMapStateUpdateEvent = e.DecodeEventDto<OnTileMapStateUpdateEvent>();
            JsonTileMapState jsonTileMapState = JsonUtility.FromJson<JsonTileMapState>(onTileMapStateUpdateEvent.State);

            this.eventActions.Enqueue(() =>
            {
                TileMapStateUpdated?.Invoke(jsonTileMapState);
            });
        }

        public static string GetAbi()
        {
            TextAsset textAsset = Resources.Load<TextAsset>("TilesChain.abi");
            if (textAsset == null)
                return null;

            return textAsset.text;
        }

        [FunctionOutput]
        public class TileMapStateOutput
        {
            [Parameter("string", "state", 1)]
            public string State { get; set; }
        }

        [Function("GetTileMapState", "string")]
        public class TileMapStateFunction
        {
            [Parameter("string", "state", 1)]
            public string State { get; set; }
        }

        public class OnTileMapStateUpdateEvent
        {
            [Parameter("string", "state", 1)]
            public string State { get; set; }
        }
    }
}
