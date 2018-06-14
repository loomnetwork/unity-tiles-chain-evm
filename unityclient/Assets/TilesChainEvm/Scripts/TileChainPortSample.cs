using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Loom.Unity3d.Samples.TilesChain2 {
    public class TileChainPortSample : MonoBehaviour {
        public Sprite PointSprite;
        public Sprite SquareSprite;
        public Vector2 GameFieldSize = new Vector2(640, 480);

        private TileChainContractClient _client;
        private JsonTileMapState _jsonTileMapState = new JsonTileMapState();
        private List<GameObject> _tileGameObjects = new List<GameObject>();
        private Color32 _color;

        // Use this for initialization
        async void Start() {
            Camera.main.orthographicSize = GameFieldSize.y / 2f;
            Camera.main.transform.position = new Vector3(GameFieldSize.x / 2f, GameFieldSize.y / 2f, Camera.main.transform.position.z);

            GameObject gameFieldGo = new GameObject("GameField");
            SpriteRenderer gameFieldSpriteRenderer = gameFieldGo.AddComponent<SpriteRenderer>();
            gameFieldSpriteRenderer.sprite = SquareSprite;
            gameFieldSpriteRenderer.sortingOrder = -1;
            gameFieldSpriteRenderer.color = Color.black;
            gameFieldGo.transform.localScale = new Vector3(GameFieldSize.x, GameFieldSize.y, 1f);
            gameFieldGo.transform.position = GameFieldSize * 0.5f;

            // Pick nice random color for this player
            _color = Random.ColorHSV(0, 1, 1, 1, 1, 1);

            // The private key is used to sign transactions sent to the DAppChain.
            // Usually you'd generate one private key per player, or let them provide their own.
            // In this sample we just generate a new key every time.
            var privateKey = CryptoUtils.GeneratePrivateKey();
            var publicKey = CryptoUtils.PublicKeyFromPrivateKey(privateKey);
            _client = new TileChainContractClient(privateKey, publicKey, Debug.unityLogger);
            _client.TileMapStateUpdated += ClientOnTileMapStateUpdated;
            JsonTileMapState jsonTileMapState = await _client.GetTileMapState();
            UpdateTileMap(jsonTileMapState);
        }

        private void ClientOnTileMapStateUpdated(JsonTileMapState obj) {
            UpdateTileMap(obj);
        }

        private void Update() {
            _client.Update();
            if (Input.GetMouseButtonDown(0)) {
                Ray screenPointToRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                Vector2 dotPosition = screenPointToRay.origin;

                if (dotPosition.x < 0f || dotPosition.x > GameFieldSize.x ||
                    dotPosition.y < 0f || dotPosition.y > GameFieldSize.y)
                    return;

                JsonTileMapState.Tile tile = new JsonTileMapState.Tile {
                    color = new JsonTileMapState.Tile.Color {
                        r = _color.r,
                        g = _color.g,
                        b = _color.b,
                    },
                    point = new Vector2Int((int) dotPosition.x, (int) GameFieldSize.y - (int) dotPosition.y)
                };
                _jsonTileMapState.tiles.Add(tile);
#pragma warning disable 4014
                _client.SetTileMapState(_jsonTileMapState);
#pragma warning restore 4014
            }
        }

        private void UpdateTileMap(JsonTileMapState jsonTileMapState) {
            _jsonTileMapState = jsonTileMapState ?? new JsonTileMapState();

            foreach (GameObject tile in _tileGameObjects) {
                Destroy(tile);
            }

            _tileGameObjects.Clear();
            foreach (JsonTileMapState.Tile tile in _jsonTileMapState.tiles) {
                GameObject go = new GameObject("Tile");
                go.transform.localScale = Vector3.one * 16f;
                go.transform.position = new Vector3(tile.point.x, 480 - tile.point.y, 0);
                SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = PointSprite;
                spriteRenderer.color = new Color32((byte) tile.color.r, (byte) tile.color.g, (byte) tile.color.b, 255);

                _tileGameObjects.Add(go);
            }
        }
    }
}