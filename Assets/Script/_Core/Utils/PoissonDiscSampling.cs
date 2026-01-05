// PoissonDiscSampling.cs
using UnityEngine;
using System.Collections.Generic;

public static class PoissonDiscSampling
{
    // --- ADDED A SAFETY LIMIT ---
    private const int maxGridSize = 1000000; // Cap grid size to 1 million cells to prevent overflow

    public static Vector2[] GeneratePoints(float radius, Rect rect, int maxCandidates = 30)
    {
        // --- ADDED A SAFETY CHECK for radius ---
        if (radius <= 0)
        {
            Debug.LogError("PoissonDiscSampling radius must be greater than 0.");
            return new Vector2[0]; // Return empty array
        }
        
        float cellSize = radius / Mathf.Sqrt(2);

        int gridWidth = Mathf.CeilToInt(rect.width / cellSize);
        int gridHeight = Mathf.CeilToInt(rect.height / cellSize);
        
        // --- ADDED A SAFETY CHECK for grid size ---
        if ((long)gridWidth * gridHeight > maxGridSize)
        {
            Debug.LogWarning($"Poisson grid size is too large ({gridWidth}x{gridHeight}). Lower density or increase area. Capping spawn.");
            return new Vector2[0]; // Return empty array to prevent crash
        }
        
        Vector2[,] grid = new Vector2[gridWidth, gridHeight];
        // ... (The rest of the script is exactly the same) ...
        bool[,] gridOccupied = new bool[gridWidth, gridHeight];

        List<Vector2> activeList = new List<Vector2>();
        List<Vector2> points = new List<Vector2>();

        Vector2 firstPoint = new Vector2(Random.Range(rect.xMin, rect.xMax), Random.Range(rect.yMin, rect.yMax));
        points.Add(firstPoint);
        activeList.Add(firstPoint);
        int xIndex = Mathf.FloorToInt((firstPoint.x - rect.xMin) / cellSize);
        int yIndex = Mathf.FloorToInt((firstPoint.y - rect.yMin) / cellSize);
        grid[xIndex, yIndex] = firstPoint;
        gridOccupied[xIndex, yIndex] = true;

        while (activeList.Count > 0)
        {
            int randomIndex = Random.Range(0, activeList.Count);
            Vector2 currentPoint = activeList[randomIndex];
            bool foundValidPoint = false;
            for (int i = 0; i < maxCandidates; i++)
            {
                float angle = Random.Range(0f, 2f * Mathf.PI);
                float distance = Random.Range(radius, 2f * radius);
                Vector2 newPoint = currentPoint + new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);

                if (!rect.Contains(newPoint))
                    continue;

                int newX = Mathf.FloorToInt((newPoint.x - rect.xMin) / cellSize);
                int newY = Mathf.FloorToInt((newPoint.y - rect.yMin) / cellSize);
                bool tooClose = false;
                for (int dx = -1; dx <= 1 && !tooClose; dx++)
                {
                    for (int dy = -1; dy <= 1 && !tooClose; dy++)
                    {
                        int neighborX = newX + dx;
                        int neighborY = newY + dy;
                        if (neighborX >= 0 && neighborX < gridWidth &&
                            neighborY >= 0 && neighborY < gridHeight &&
                            gridOccupied[neighborX, neighborY])
                        {
                            float dist = Vector2.Distance(newPoint, grid[neighborX, neighborY]);
                            if (dist < radius)
                                tooClose = true;
                        }
                    }
                }
                if (!tooClose)
                {
                    points.Add(newPoint);
                    activeList.Add(newPoint);
                    grid[newX, newY] = newPoint;
                    gridOccupied[newX, newY] = true;
                    foundValidPoint = true;
                    break;
                }
            }
            if (!foundValidPoint)
            {
                activeList.RemoveAt(randomIndex);
            }
        }
        return points.ToArray();
    }
}