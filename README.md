# Unity Tiles Chain EVM  Sample

A basic example showcasing a simple Unity game interacting with an EVM-based Loom DappChain, using [Loom Unity SDK](https://github.com/loomnetwork/unity3d-sdk).

It uses `Truffle` and [Loom Truffle Provider](https://github.com/loomnetwork/loom-truffle-provider) for deployment and contract testing.

![](https://camo.githubusercontent.com/9d49b0ce78d692e69d1dd571bc8d1aafe5b806a8/68747470733a2f2f647a776f6e73656d72697368372e636c6f756466726f6e742e6e65742f6974656d732f315232363044327030713370304d33693232304a2f53637265656e2532305265636f7264696e67253230323031382d30352d3232253230617425323031302e3233253230414d2e6769663f763d3961353539316139)


Game instructions
----

Use the mouse cursor to click on the black canvas area to create colored tiles, each new player will have a different color the canvas which is shared amongst all players.

Development
----

## 1. Run your own DappChain

Please consult the [Loom SDK docs](https://loomx.io/developers/docs/en/prereqs.html) for further instruction on running your own DappChain.

## 2. Download the example project (Tiles Chain EVM)

```bash
git clone https://github.com/loomnetwork/unity-tiles-chain-evm
```

## 3. Start the DappChain local node

Open a console.

```bash
cd unity-tiles-chain-evm
cd DAppChain

# Configure and Run
./start-chain.sh
```

## 4. Build and Deploy with Truffle

Open another console.

```bash
cd unity-tiles-chain-evm
cd TruffleProject

# Restore packages
npm install

# Build and copy contract ABI into Unity project
npm run build

# Deploy to local node
npm run deploy
```

After deployment, take note of the address of the deployed contract.

## 5. Build the Unity client
1. Open the Unity project located in `UnityClient`.
2. Open the `LoomTilesChainEvm` scene.
3. Select the `Controller` object and copy the deployed contract address into the `ContractAddressHex` field.
4. Run the scene to check if everything works, and build the project.

Loom Network
----
[https://loomx.io](https://loomx.io)


License
----

MIT
