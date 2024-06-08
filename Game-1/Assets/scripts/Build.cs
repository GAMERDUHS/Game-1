using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class Build : MonoBehaviour
{
    public Tilemap tilemap;
    public List<TileBase> placeableTiles; // List of placeable tiles
    public GameObject buttonPrefab; // Prefab for UI buttons
    public Transform buttonContainer; // Container for buttons in the UI

    private TileBase selectedTile; // Currently selected tile

    public static event Action<Vector3Int, TileBase> OnTilePlaced;
    public static event Action<Vector3Int> OnTileRemoved;

    void Start()
    {
        selectedTile = placeableTiles[0]; // Start with the first tile in the list selected

        // Initialize the UI buttons
        for (int i = 0; i < placeableTiles.Count; i++)
        {
            int index = i; // Local copy of the loop variable for the lambda
            GameObject button = Instantiate(buttonPrefab, buttonContainer);
            button.GetComponentInChildren<Text>().text = placeableTiles[i].name; // Set button text to tile name
            button.GetComponent<Button>().onClick.AddListener(() => SelectTile(index));
        }

        UpdateButtonSelection();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridPos = tilemap.WorldToCell(mousePos);

            PlaceTile(gridPos);
        }

        if (Input.GetMouseButtonDown(1)) // Right mouse button
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridPos = tilemap.WorldToCell(mousePos);

            RemoveTile(gridPos);
        }
    }

    void PlaceTile(Vector3Int gridPos)
    {
        tilemap.SetTile(gridPos, selectedTile);
        OnTilePlaced?.Invoke(gridPos, selectedTile);
    }

    void RemoveTile(Vector3Int gridPos)
    {
        tilemap.SetTile(gridPos, null);
        OnTileRemoved?.Invoke(gridPos);
    }

    // Method to select the tile from UI button
    public void SelectTile(int index)
    {
        selectedTile = placeableTiles[index];
        UpdateButtonSelection();
    }

    // Update button selection to visually indicate the selected tile
    private void UpdateButtonSelection()
    {
        foreach (Transform child in buttonContainer)
        {
            Button button = child.GetComponent<Button>();
            if (button != null)
            {
                int index = button.transform.GetSiblingIndex();
                ColorBlock colors = button.colors;
                if (placeableTiles[index] == selectedTile)
                {
                    colors.normalColor = Color.yellow;
                    colors.highlightedColor = Color.yellow;
                }
                else
                {
                    colors.normalColor = Color.white;
                    colors.highlightedColor = Color.cyan;
                }
                button.colors = colors;
            }
        }
    }
}
