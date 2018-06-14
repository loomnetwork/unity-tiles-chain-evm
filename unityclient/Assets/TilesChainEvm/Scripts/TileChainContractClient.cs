using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Loom.Unity3d;
using Loom.Nethereum.ABI.Decoders;
using Loom.Nethereum.ABI.FunctionEncoding;
using Loom.Nethereum.ABI.FunctionEncoding.Attributes;
using Loom.Nethereum.ABI.Model;
using Loom.Nethereum.Contracts;
using Loom.Nethereum.RPC.Eth.DTOs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Loom.Unity3d.Samples.TilesChain2 {
    public class TileChainContractClient {
        private readonly byte[] _privateKey;
        private readonly byte[] _publicKey;
        private readonly ILogger _logger;
        private EvmContract _contract;
        private Queue<Action> _eventActions = new Queue<Action>();

        public event Action<JsonTileMapState> TileMapStateUpdated;

        public TileChainContractClient(byte[] privateKey, byte[] publicKey, ILogger logger) {
            _privateKey = privateKey;
            _publicKey = publicKey;
            _logger = logger;
        }

        public void Update() {
            while (_eventActions.Count > 0) {
                Action action = _eventActions.Dequeue();
                action();
            }
        }

        private async Task<EvmContract> GetContract(byte[] privateKey, byte[] publicKey) {
            var writer = RPCClientFactory.Configure()
                .WithLogger(Debug.unityLogger)
                .WithWebSocket("ws://127.0.0.1:46657/websocket")
                .Create();

            var reader = RPCClientFactory.Configure()
                .WithLogger(Debug.unityLogger)
                .WithWebSocket("ws://127.0.0.1:9999/queryws")
                .Create();

            var client = new DAppChainClient(writer, reader)
                { Logger = _logger };

            // required middleware
            client.TxMiddleware = new TxMiddleware(new ITxMiddlewareHandler[] {
                new NonceTxMiddleware {
                    PublicKey = publicKey,
                    Client = client
                },
                new SignedTxMiddleware(privateKey)
            });

            const string abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_tileState\",\"type\":\"string\"}],\"name\":\"SetTileMapState\",\"outputs\":[],\"payable\":false,\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"GetTileMapState\",\"outputs\":[{\"name\":\"\",\"type\":\"string\"}],\"payable\":false,\"stateMutability\":\"view\",\"type\":\"function\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":false,\"name\":\"state\",\"type\":\"string\"}],\"name\":\"OnTileMapStateUpdate\",\"type\":\"event\"}]\r\n";

            var contractAddr = await client.ResolveContractAddressAsync("TilesChain");
            var callerAddr = Address.FromPublicKey(publicKey);
            EvmContract evmContract = new EvmContract(client, contractAddr, callerAddr, abi);
            evmContract.OnEvent += ClientOnOnChainEvent;
            return evmContract;
        }

        private void ClientOnOnChainEvent(object sender, DAppChainClient.ChainEventArgs e) {
            // TODO: Need to check the event name to know how to decode it.
            // Currently there is no way, so just assume the needed event
            ParameterDecoder parameterDecoder = new ParameterDecoder();
            List<ParameterOutput> decodedData =
                parameterDecoder.DecodeDefaultData(CryptoUtils.BytesToHexString(e.Data), new Parameter("string", "state"));

            JsonTileMapState jsonTileMapState = JsonUtility.FromJson<JsonTileMapState>((string) decodedData[0].Result);
            _eventActions.Enqueue(() => {
                TileMapStateUpdated?.Invoke(jsonTileMapState);
            });
        }

        public async Task<JsonTileMapState> GetTileMapState() {
            if (_contract == null) {
                _contract = await GetContract(_privateKey, _publicKey);
            }

            TileMapStateOutput result = await _contract.StaticCallDTOTypeOutputAsync<TileMapStateOutput>("GetTileMapState");
            if (result != null) {
                JsonTileMapState jsonTileMapState = JsonUtility.FromJson<JsonTileMapState>(result.State);
                return jsonTileMapState;
            } else {
                throw new Exception("Smart contract didn't return anything!");
            }
        }

        public async Task SetTileMapState(JsonTileMapState jsonTileMapState) {
            if (_contract == null) {
                _contract = await GetContract(_privateKey, _publicKey);
            }

            TileMapState tileMapStateTx = new TileMapState();
            tileMapStateTx.Data = JsonUtility.ToJson(jsonTileMapState);
            await _contract.CallAsync("SetTileMapState", tileMapStateTx.Data);
        }

        [FunctionOutput]
        public class TileMapStateOutput
        {
            [Parameter("string", "state", 1)]
            public string State {get; set;}
        }

        [Function("GetTileMapState", "string")]
        public class TileMapStateFunction
        {
            [Parameter("string", "state", 1)]
            public string State {get; set;}
        }
    }
}