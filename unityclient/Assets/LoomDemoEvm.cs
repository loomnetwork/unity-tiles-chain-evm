using UnityEngine;
using UnityEngine.UI;
using Loom.Client;
using System;
using Loom.Nethereum.ABI.FunctionEncoding.Attributes;
using Random = UnityEngine.Random;

public class LoomDemoEvm : MonoBehaviour
{
    public Text statusTextRef;

    private EvmContract contract;

    public class OnTileMapStateUpdateEvent
    {
        [Parameter("string", "state", 1)]
        public string State { get; set; }
    }

    [FunctionOutput]
    public class TileMapStateOutput
    {
        [Parameter("string", "state", 1)]
        public string State { get; set; }
    }

    // Use this for initialization
    void Start()
    {
        // By default the editor won't respond to network IO or anything if it doesn't have input focus,
        // which is super annoying when input focus is given to the web browser for the Auth0 sign-in.
        Application.runInBackground = true;
    }

    public async void SignIn()
    {
        var privateKey = CryptoUtils.GeneratePrivateKey();
        var publicKey = CryptoUtils.PublicKeyFromPrivateKey(privateKey);
        var callerAddr = Address.FromPublicKey(publicKey);
        this.statusTextRef.text = "Signed in as " + callerAddr.QualifiedAddress;

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

        const string abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_tileState\",\"type\":\"string\"}],\"name\":\"SetTileMapState\",\"outputs\":[],\"payable\":false,\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"GetTileMapState\",\"outputs\":[{\"name\":\"\",\"type\":\"string\"}],\"payable\":false,\"stateMutability\":\"view\",\"type\":\"function\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":false,\"name\":\"state\",\"type\":\"string\"}],\"name\":\"OnTileMapStateUpdate\",\"type\":\"event\"}]\r\n";
        var contractAddr = await client.ResolveContractAddressAsync("TilesChain");

        contract = new EvmContract(client, contractAddr, callerAddr, abi);

        // Subscribe to DAppChainClient.ChainEventReceived to receive events from a specific smart contract
        this.contract.EventReceived += (sender, e) =>
        {
            OnTileMapStateUpdateEvent tileMapStateUpdateEvent = e.DecodeEventDto<OnTileMapStateUpdateEvent>();
            Debug.Log(string.Format("Contract Event: {0}, {1}, from block {2}", e.EventName, tileMapStateUpdateEvent.State, e.BlockHeight));
        };
    }

    public async void CallContractWithResult()
    {
        if (this.contract == null)
        {
            throw new Exception("Not signed in!");
        }

        this.statusTextRef.text = "Calling smart contract...";

        await this.contract.CallAsync("SetTileMapState", "hello " + Random.Range(0, 10000));

        this.statusTextRef.text = "Smart contract method finished executing.";
    }

    public async void StaticCallContract()
    {
        if (this.contract == null)
        {
            throw new Exception("Not signed in!");
        }

        this.statusTextRef.text = "Calling smart contract...";

        TileMapStateOutput result = await this.contract.StaticCallDtoTypeOutputAsync<TileMapStateOutput>("GetTileMapState");
        if (result != null)
        {
            this.statusTextRef.text = "Smart contract returned: " + result.State;
        } else
        {
            this.statusTextRef.text = "Smart contract didn't return anything!";
        }
    }
}
