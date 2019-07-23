const fs = require("fs");
const path = require("path");
const { join } = require("path");
const LoomTruffleProvider = require("loom-truffle-provider");
const LoomUnityBuildUtility = require("./LoomUnityBuildUtility");

function getLoomProviderWithPrivateKey(privateKeyPath, chainId, writeUrl, readUrl) {
  const privateKey = fs.readFileSync(privateKeyPath, "utf-8");
  return new LoomTruffleProvider(chainId, writeUrl, readUrl, privateKey);
}

function getLoomProviderWithMnemonic(mnemonicPath, chainId, writeUrl, readUrl) {
  const mnemonic = fs.readFileSync(mnemonicPath, "utf-8").toString().trim();
  const seed = mnemonicToSeedSync(mnemonic);
  const privateKeyUint8ArrayFromSeed = CryptoUtils.generatePrivateKeyFromSeed(new Uint8Array(sha256.array(seed)));
  const privateKeyB64 = CryptoUtils.Uint8ArrayToB64(privateKeyUint8ArrayFromSeed);
  return new LoomTruffleProvider(chainId, writeUrl, readUrl, privateKeyB64);
}

module.exports = {
  // See <http://truffleframework.com/docs/advanced/configuration>
  // to customize your Truffle configuration!
  compilers: {
    solc: {
      version: "0.4.24"
    }
  },
  networks: {
    loom_dapp_chain: {
      provider: function() {
        const chainId = "default";
        const writeUrl = "http://127.0.0.1:46658/rpc";
        const readUrl = "http://127.0.0.1:46658/query";
        const mnemonicPath = path.join(__dirname, "loom_mnemonic");
        const privateKeyPath = path.join(__dirname, "loom_private_key");
        if (fs.existsSync(privateKeyPath)) {
          const loomTruffleProvider = getLoomProviderWithPrivateKey(privateKeyPath, chainId, writeUrl, readUrl);
          loomTruffleProvider.createExtraAccountsFromMnemonic("gravity top burden flip student usage spell purchase hundred improve check genre", 10);
          return loomTruffleProvider;
        } else if (fs.existsSync(mnemonicPath)) {
          const loomTruffleProvider = getLoomProviderWithMnemonic(mnemonicPath, chainId, writeUrl, readUrl);
          return loomTruffleProvider;
        }
      },
      network_id: "*"
    }
  },
  build: function(options, callback) {
    new LoomUnityBuildUtility(options, ["TilesChain"], "../UnityClient/Assets/TilesChainEvm/Resources/").copyFiles();
  }
};
