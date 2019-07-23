using UnityEngine;
using System;
using System.Threading.Tasks;
using Loom.Client;
using Loom.Nethereum.ABI.FunctionEncoding.Attributes;

public class LoomEvmQuickStartSample : MonoBehaviour
{
    public class OnTileMapStateUpdateEvent
    {
        [Parameter("string", "state", 1)]
        public string State { get; set; }
    }

    async void Start()
    {
        // The private key is used to sign transactions sent to the DAppChain.
        // Usually you'd generate one private key per player, or let them provide their own.
        // In this sample we just generate a new key every time.
        var privateKey = CryptoUtils.GeneratePrivateKey();
        var publicKey = CryptoUtils.PublicKeyFromPrivateKey(privateKey);

        // Connect to the contract
        var contract = await GetContract(privateKey, publicKey);
        // This should print something like: "hello 6475" in the Unity console window if some data is already stored
        await StaticCallContract(contract);
        // Listen for events
        contract.EventReceived += this.ContractEventReceived;
        await contract.Client.SubscribeToAllEvents();
        // Store the string in a contract
        await CallContract(contract);
    }

    private void ContractEventReceived(object sender, EvmChainEventArgs e)
    {
        Debug.LogFormat("Received smart contract event: " + e.EventName);
        if (e.EventName == "OnTileMapStateUpdate")
        {
            OnTileMapStateUpdateEvent onTileMapStateUpdateEvent = e.DecodeEventDto<OnTileMapStateUpdateEvent>();
            Debug.LogFormat("OnTileMapStateUpdate event data: " + onTileMapStateUpdateEvent.State);
        }
    }

    async Task<EvmContract> GetContract(byte[] privateKey, byte[] publicKey)
    {
        var writer = RpcClientFactory.Configure()
            .WithLogger(Debug.unityLogger)
            .WithWebSocket("ws://127.0.0.1:46658/websocket")
            .Create();

        var reader = RpcClientFactory.Configure()
            .WithLogger(Debug.unityLogger)
            .WithWebSocket("ws://127.0.0.1:46658/queryws")
            .Create();

        var client = new DAppChainClient(writer, reader)
            { Logger = Debug.unityLogger };

        // required middleware
        client.TxMiddleware = new TxMiddleware(new ITxMiddlewareHandler[]
        {
            new NonceTxMiddleware(publicKey, client),
            new SignedTxMiddleware(privateKey)
        });

        // ABI of the Solidity contract
        const string abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_tileState\",\"type\":\"string\"}],\"name\":\"SetTileMapState\",\"outputs\":[],\"payable\":false,\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"GetTileMapState\",\"outputs\":[{\"name\":\"\",\"type\":\"string\"}],\"payable\":false,\"stateMutability\":\"view\",\"type\":\"function\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":false,\"name\":\"state\",\"type\":\"string\"}],\"name\":\"OnTileMapStateUpdate\",\"type\":\"event\"}]\r\n";
        var contractAddr = await client.ResolveContractAddressAsync("TilesChain");
        var callerAddr = Address.FromPublicKey(publicKey);

        return new EvmContract(client, contractAddr, callerAddr, abi);
    }

    public async Task CallContract(EvmContract contract)
    {
        if (contract == null)
        {
            throw new Exception("Not signed in!");
        }

        Debug.Log("Calling smart contract...");

        await contract.CallAsync("SetTileMapState", "hello " + UnityEngine.Random.Range(0, 10000));

        Debug.Log("Smart contract method finished executing.");
    }

    public async Task StaticCallContract(EvmContract contract)
    {
        if (contract == null)
        {
            throw new Exception("Not signed in!");
        }

        Debug.Log("Calling smart contract...");

        string result = await contract.StaticCallSimpleTypeOutputAsync<string>("GetTileMapState");
        if (result != null)
        {
            Debug.Log("Smart contract returned: " + result);
        } else
        {
            Debug.LogError("Smart contract didn't return anything!");
        }
    }
}
