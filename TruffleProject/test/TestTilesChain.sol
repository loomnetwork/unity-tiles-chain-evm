pragma solidity ^0.4.24;

import "truffle/Assert.sol";
import "truffle/DeployedAddresses.sol";
import "../contracts/TilesChain.sol";

contract TestBlueprint {
    function testTileMapState() public {
        TilesChain tiles = TilesChain(DeployedAddresses.TilesChain());
        Assert.equal(tiles.GetTileMapState(), "", "initial state must be empty");

        tiles.SetTileMapState("test");
        Assert.equal(tiles.GetTileMapState(), "test", "state must be equal to 'test'");
    }
}