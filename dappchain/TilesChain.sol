pragma solidity ^0.4.22;

contract TilesChain {
  string tileState;

  event OnTileMapStateUpdate(string state);

  function SetTileMapState(string _tileState) public {
    tileState = _tileState;
    emit OnTileMapStateUpdate(tileState);
  }

  function GetTileMapState() public view returns(string) {
    return tileState;
  }
}
