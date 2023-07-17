using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This static class is used to generate meshes representing the duals of geodesic isohedrons by perfoming dual and truncation algorithms alternatively.
/// </summary>
public static class DualGeodesicIsohedronGenerator
{
    /// <summary>
    /// Beginning with a tetrahedral seed, perform the dual/truncation algorithm to generate a shape containing 4 evenly distributed triangles and a certain number of hexagons.
    /// </summary>
    /// <param name="complexity">The number of times to perform the algorithm (note: passing 0 will return the original tetrahedral seed unchanged).</param>
    /// <returns>A tetrahedral mesh with the algorithm applied to it the specified number of times.</returns>
    public static Mesh GenerateDualGeodesicTetrahedron(int complexity)
    {
        List<Vector3> vertices = new List<Vector3>() { (new Vector3(-Mathf.Sqrt(2), 0, -1)).normalized, (new Vector3(Mathf.Sqrt(2), 0, -1)).normalized, (new Vector3(0, -Mathf.Sqrt(2), 1)).normalized, (new Vector3(0, Mathf.Sqrt(2), 1)).normalized };
        List<int> triangles = new List<int>() { 0, 1, 3, 0, 2, 1, 0, 3, 2, 1, 2, 3 };
        List<int> hexagons = new List<int>();
        for (int i = 0; i < complexity; i++)
        {
            // transform the current shape into its dual counterpart (made entirely of triangles)
            List<Vector3> dualVertices = new List<Vector3>();
            List<int> dualTriangles = new List<int>();
            for (int j = 0; j < vertices.Count; j++) // for each vertex of the original shape
            {
                List<int> adjacentFaces = new List<int>();
                List<int> previousPoints = new List<int>();
                List<int> nextPoints = new List<int>();
                for (int k = 0; k < triangles.Count; k++) // for each triangle of the original shape
                {
                    if (j == triangles[k]) // if the current triangle contains the current point
                    {
                        adjacentFaces.Add(k / 3);
                        previousPoints.Add(triangles[k % 3 == 0 ? k + 2 : k - 1]);
                        nextPoints.Add(triangles[k % 3 < 2 ? k + 1 : k - 2]);
                    }
                }
                for (int k = 0; k < hexagons.Count; k++) // for each hexagon of the original shape
                {
                    if (j == hexagons[k]) // if the current hexagon contains the current point
                    {
                        adjacentFaces.Add(triangles.Count / 3 + k / 6);
                        previousPoints.Add(hexagons[k % 6 == 0 ? k + 5 : k - 1]);
                        nextPoints.Add(hexagons[k % 6 < 5 ? k + 1 : k - 5]);
                    }
                }
                if (previousPoints[0] == nextPoints[1] && previousPoints[1] == nextPoints[2] && previousPoints[2] == nextPoints[0]) // if the 3 new vertices were found in clockwise order
                {
                    dualTriangles.Add(adjacentFaces[0]);
                    dualTriangles.Add(adjacentFaces[1]);
                    dualTriangles.Add(adjacentFaces[2]);
                }
                else if (previousPoints[0] == nextPoints[2] && previousPoints[1] == nextPoints[0] && previousPoints[2] == nextPoints[1]) // if the 3 new vertices were found in anticlockwise order
                {
                    dualTriangles.Add(adjacentFaces[0]);
                    dualTriangles.Add(adjacentFaces[2]);
                    dualTriangles.Add(adjacentFaces[1]);
                }
            }
            Vector3 newPoint = new Vector3(0, 0, 0);
            for (int j = 0; j < triangles.Count; j++) // add a new point at the centre of each triangle of the original shape and project it onto the unit sphere
            {
                newPoint += vertices[triangles[j]] / 3;
                if (j % 3 == 2)
                {
                    dualVertices.Add(newPoint.normalized);
                    newPoint = new Vector3(0, 0, 0);
                }
            }
            for (int j = 0; j < hexagons.Count; j++) // add a new point at the centre of each hexagon of the original shape and project it onto the unit sphere
            {
                newPoint += vertices[hexagons[j]] / 6;
                if (j % 6 == 5)
                {
                    dualVertices.Add(newPoint.normalized);
                    newPoint = new Vector3(0, 0, 0);
                }
            }
            List<Vector3> truncatedVertices = new List<Vector3>();
            List<int> truncatedTriangles = new List<int>();
            List<int> truncatedHexagons = new List<int>();
            List<int> dualEdges = new List<int>();
            List<int> truncatedEdges = new List<int>();
            for (int j = 0; j < dualVertices.Count; j++) // for each vertex of the dual shape
            {
                List<int> adjacentFaces = new List<int>();
                List<int> previousPoints = new List<int>();
                List<int> nextPoints = new List<int>();
                for (int k = 0; k < dualTriangles.Count; k++) // for each triangle of the dual shape
                {
                    if (j == dualTriangles[k]) // if the current triangle contains the current point
                    {
                        adjacentFaces.Add(k / 3);
                        previousPoints.Add(dualTriangles[k % 3 == 0 ? k + 2 : k - 1]);
                        nextPoints.Add(dualTriangles[k % 3 < 2 ? k + 1 : k - 2]);
                    }
                }
                int currentIndex = 0;
                do // find next vertex in cycle
                {
                    bool foundNext = false;
                    for (int k = 0; k < adjacentFaces.Count; k++)
                    {
                        if (!foundNext && k != currentIndex && previousPoints[currentIndex] == nextPoints[k])
                        {
                            dualEdges.Add(j);
                            dualEdges.Add(nextPoints[currentIndex]);
                            truncatedEdges.Add(truncatedVertices.Count);
                            if (adjacentFaces.Count == 3) truncatedTriangles.Add(truncatedVertices.Count);
                            else if (adjacentFaces.Count == 6) truncatedHexagons.Add(truncatedVertices.Count);
                            truncatedVertices.Add((dualVertices[j] + (dualVertices[nextPoints[currentIndex]] - dualVertices[j]) / 3).normalized); // add new vertex a third of the way along the edge between the two vertices and project it onto the unit sphere
                            currentIndex = k;
                            foundNext = true;
                        }
                    }
                }
                while (currentIndex != 0);
            }
            for (int j = 0; j < dualTriangles.Count; j++) // for each triangle of the dual shape
            {
                List<int> adjacentDualVertices = new List<int>();
                List<int> adjacentTruncatedVertices = new List<int>();
                for (int k = 0; k < dualEdges.Count; k += 2)
                {
                    if (dualTriangles[j] == dualEdges[k])
                    {
                        adjacentDualVertices.Add(dualEdges[k + 1]);
                        adjacentTruncatedVertices.Add(truncatedEdges[k / 2]);
                    }
                }
                for (int k = 0; k < adjacentDualVertices.Count; k++)
                {
                    if (dualTriangles[j % 3 < 2 ? j + 1 : j - 2] == adjacentDualVertices[k] && dualTriangles[j % 3 == 0 ? j + 2 : j - 1] == adjacentDualVertices[(k + 1) % adjacentDualVertices.Count]) // add these two vertices to current hexagon
                    {
                        truncatedHexagons.Add(adjacentTruncatedVertices[(k + 1) % adjacentTruncatedVertices.Count]);
                        truncatedHexagons.Add(adjacentTruncatedVertices[k]);
                    }
                }
            }
            // overwrite data with that of truncated shape
            vertices = truncatedVertices;
            triangles = truncatedTriangles;
            hexagons = truncatedHexagons;
        }
        Vector3 newVertex = new Vector3(0, 0, 0);
        for (int i = 0; i < hexagons.Count; i++) // place new vertex in the centre of each hexagonal face and connect it to the edges with triangles
        {
            newVertex += vertices[hexagons[i]] / 6;
            triangles.Add(hexagons[i]);
            if (i % 6 < 5)
            {
                triangles.Add(hexagons[i + 1]);
                triangles.Add(vertices.Count);
            }
            else
            {
                triangles.Add(hexagons[i - 5]);
                triangles.Add(vertices.Count);
                vertices.Add(newVertex);
                newVertex = new Vector3(0, 0, 0);
            }
        }
        return new Mesh()
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray()
        };
    }

    /// <summary>
    /// Beginning with a cubic seed, perform the dual/truncation algorithm to generate a shape containing 6 evenly distributed squares and a certain number of hexagons.
    /// </summary>
    /// <param name="complexity">The number of times to perform the algorithm (note: passing 0 will return the original cubic seed unchanged).</param>
    /// <returns>A cubic mesh with the algorithm applied to it the specified number of times.</returns>
    public static Mesh GenerateDualGeodesicOctahedron(int complexity)
    {
        List<Vector3> vertices = new List<Vector3>() { (new Vector3(-1, -1, -1)).normalized, (new Vector3(-1, -1, 1)).normalized, (new Vector3(-1, 1, -1)).normalized, (new Vector3(-1, 1, 1)).normalized, (new Vector3(1, -1, -1)).normalized, (new Vector3(1, -1, 1)).normalized, (new Vector3(1, 1, -1)).normalized, (new Vector3(1, 1, 1)).normalized };
        List<int> squares = new List<int>() { 0, 1, 3, 2, 0, 4, 5, 1, 0, 2, 6, 4, 1, 5, 7, 3, 2, 3, 7, 6, 4, 6, 7, 5 };
        List<int> hexagons = new List<int>();
        for (int i = 0; i < complexity; i++)
        {
            // transform the current shape into its dual counterpart (made entirely of triangles)
            List<Vector3> dualVertices = new List<Vector3>();
            List<int> dualTriangles = new List<int>();
            for (int j = 0; j < vertices.Count; j++) // for each vertex of the original shape
            {
                List<int> adjacentFaces = new List<int>();
                List<int> previousPoints = new List<int>();
                List<int> nextPoints = new List<int>();
                for (int k = 0; k < squares.Count; k++) // for each square of the original shape
                {
                    if (j == squares[k]) // if the current square contains the current point
                    {
                        adjacentFaces.Add(k / 4);
                        previousPoints.Add(squares[k % 4 == 0 ? k + 3 : k - 1]);
                        nextPoints.Add(squares[k % 4 < 3 ? k + 1 : k - 3]);
                    }
                }
                for (int k = 0; k < hexagons.Count; k++) // for each hexagon of the original shape
                {
                    if (j == hexagons[k]) // if the current hexagon contains the current point
                    {
                        adjacentFaces.Add(squares.Count / 4 + k / 6);
                        previousPoints.Add(hexagons[k % 6 == 0 ? k + 5 : k - 1]);
                        nextPoints.Add(hexagons[k % 6 < 5 ? k + 1 : k - 5]);
                    }
                }
                if (previousPoints[0] == nextPoints[1] && previousPoints[1] == nextPoints[2] && previousPoints[2] == nextPoints[0]) // if the 3 new vertices were found in clockwise order
                {
                    dualTriangles.Add(adjacentFaces[0]);
                    dualTriangles.Add(adjacentFaces[1]);
                    dualTriangles.Add(adjacentFaces[2]);
                }
                else if (previousPoints[0] == nextPoints[2] && previousPoints[1] == nextPoints[0] && previousPoints[2] == nextPoints[1]) // if the 3 new vertices were found in anticlockwise order
                {
                    dualTriangles.Add(adjacentFaces[0]);
                    dualTriangles.Add(adjacentFaces[2]);
                    dualTriangles.Add(adjacentFaces[1]);
                }
            }
            Vector3 newPoint = new Vector3(0, 0, 0);
            for (int j = 0; j < squares.Count; j++) // add a new point at the centre of each square of the original shape and project it onto the unit sphere
            {
                newPoint += vertices[squares[j]] / 4;
                if (j % 4 == 3)
                {
                    dualVertices.Add(newPoint.normalized);
                    newPoint = new Vector3(0, 0, 0);
                }
            }
            for (int j = 0; j < hexagons.Count; j++) // add a new point at the centre of each hexagon of the original shape and project it onto the unit sphere
            {
                newPoint += vertices[hexagons[j]] / 6;
                if (j % 6 == 5)
                {
                    dualVertices.Add(newPoint.normalized);
                    newPoint = new Vector3(0, 0, 0);
                }
            }
            List<Vector3> truncatedVertices = new List<Vector3>();
            List<int> truncatedSquares = new List<int>();
            List<int> truncatedHexagons = new List<int>();
            List<int> dualEdges = new List<int>();
            List<int> truncatedEdges = new List<int>();
            for (int j = 0; j < dualVertices.Count; j++) // for each vertex of the dual shape
            {
                List<int> adjacentFaces = new List<int>();
                List<int> previousPoints = new List<int>();
                List<int> nextPoints = new List<int>();
                for (int k = 0; k < dualTriangles.Count; k++) // for each triangle of the dual shape
                {
                    if (j == dualTriangles[k]) // if the current triangle contains the current point
                    {
                        adjacentFaces.Add(k / 3);
                        previousPoints.Add(dualTriangles[k % 3 == 0 ? k + 2 : k - 1]);
                        nextPoints.Add(dualTriangles[k % 3 < 2 ? k + 1 : k - 2]);
                    }
                }
                int currentIndex = 0;
                do // find next vertex in cycle
                {
                    bool foundNext = false;
                    for (int k = 0; k < adjacentFaces.Count; k++)
                    {
                        if (!foundNext && k != currentIndex && previousPoints[currentIndex] == nextPoints[k])
                        {
                            dualEdges.Add(j);
                            dualEdges.Add(nextPoints[currentIndex]);
                            truncatedEdges.Add(truncatedVertices.Count);
                            if (adjacentFaces.Count == 4) truncatedSquares.Add(truncatedVertices.Count);
                            else if (adjacentFaces.Count == 6) truncatedHexagons.Add(truncatedVertices.Count);
                            truncatedVertices.Add((dualVertices[j] + (dualVertices[nextPoints[currentIndex]] - dualVertices[j]) / 3).normalized); // add new vertex a third of the way along the edge between the two vertices and project it onto the unit sphere
                            currentIndex = k;
                            foundNext = true;
                        }
                    }
                }
                while (currentIndex != 0);
            }
            for (int j = 0; j < dualTriangles.Count; j++) // for each triangle of the dual shape
            {
                List<int> adjacentDualVertices = new List<int>();
                List<int> adjacentTruncatedVertices = new List<int>();
                for (int k = 0; k < dualEdges.Count; k += 2)
                {
                    if (dualTriangles[j] == dualEdges[k])
                    {
                        adjacentDualVertices.Add(dualEdges[k + 1]);
                        adjacentTruncatedVertices.Add(truncatedEdges[k / 2]);
                    }
                }
                for (int k = 0; k < adjacentDualVertices.Count; k++)
                {
                    if (dualTriangles[j % 3 < 2 ? j + 1 : j - 2] == adjacentDualVertices[k] && dualTriangles[j % 3 == 0 ? j + 2 : j - 1] == adjacentDualVertices[(k + 1) % adjacentDualVertices.Count]) // add these two vertices to current hexagon
                    {
                        truncatedHexagons.Add(adjacentTruncatedVertices[(k + 1) % adjacentTruncatedVertices.Count]);
                        truncatedHexagons.Add(adjacentTruncatedVertices[k]);
                    }
                }
            }
            // overwrite data with that of truncated shape
            vertices = truncatedVertices;
            squares = truncatedSquares;
            hexagons = truncatedHexagons;
        }
        List<int> triangles = new List<int>();
        Vector3 newVertex = new Vector3(0, 0, 0);
        for (int i = 0; i < squares.Count; i++) // place new vertex in the centre of each square face and connect it to the edges with triangles
        {
            newVertex += vertices[squares[i]] / 4;
            triangles.Add(squares[i]);
            if (i % 4 < 3)
            {
                triangles.Add(squares[i + 1]);
                triangles.Add(vertices.Count);
            }
            else
            {
                triangles.Add(squares[i - 3]);
                triangles.Add(vertices.Count);
                vertices.Add(newVertex);
                newVertex = new Vector3(0, 0, 0);
            }
        }
        for (int i = 0; i < hexagons.Count; i++) // place new vertex in the centre of each hexagonal face and connect it to the edges with triangles
        {
            newVertex += vertices[hexagons[i]] / 6;
            triangles.Add(hexagons[i]);
            if (i % 6 < 5)
            {
                triangles.Add(hexagons[i + 1]);
                triangles.Add(vertices.Count);
            }
            else
            {
                triangles.Add(hexagons[i - 5]);
                triangles.Add(vertices.Count);
                vertices.Add(newVertex);
                newVertex = new Vector3(0, 0, 0);
            }
        }
        return new Mesh()
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray()
        };
    }

    /// <summary>
    /// Beginning with a dodecahedral seed, perform the dual/truncation algorithm to generate a shape containing 12 evenly distributed pentagons and a certain number of hexagons.
    /// </summary>
    /// <param name="complexity">The number of times to perform the algorithm (note: passing 0 will return the original dodecahedral seed unchanged).</param>
    /// <returns>A dodecahedral mesh with the algorithm applied to it the specified number of times.</returns>
    public static Mesh GenerateDualGeodesicIcosahedron(int complexity)
    {
        float c = (3 + Mathf.Sqrt(5)) / 4;
        List<Vector3> vertices = new List<Vector3>() { new Vector3(0, 0.5f, c).normalized, new Vector3(0, 0.5f, -c).normalized, new Vector3(0, -0.5f, c).normalized, new Vector3(0, -0.5f, -c).normalized, new Vector3(c, 0, 0.5f).normalized, new Vector3(c, 0, -0.5f).normalized, new Vector3(-c, 0, 0.5f).normalized, new Vector3(-c, 0, -0.5f).normalized, new Vector3(0.5f, c, 0).normalized, new Vector3(0.5f, -c, 0).normalized, new Vector3(-0.5f, c, 0).normalized, new Vector3(-0.5f, -c, 0).normalized, new Vector3(1, 1, 1).normalized, new Vector3(1, 1, -1).normalized, new Vector3(1, -1, 1).normalized, new Vector3(1, -1, -1).normalized, new Vector3(-1, 1, 1).normalized, new Vector3(-1, 1, -1).normalized, new Vector3(-1, -1, 1).normalized, new Vector3(-1, -1, -1).normalized };
        List<int> pentagons = new List<int>() { 0, 2, 14, 4, 12, 0, 12, 8, 10, 16, 0, 16, 6, 18, 2, 6, 16, 10, 17, 7, 1, 3, 19, 7, 17, 6, 7, 19, 11, 18, 3, 15, 9, 11, 19, 4, 14, 9, 15, 5, 2, 18, 11, 9, 14, 1, 17, 10, 8, 13, 4, 5, 13, 8, 12, 1, 13, 5, 15, 3 };
        List<int> hexagons = new List<int>();
        for (int i = 0; i < complexity; i++)
        {
            // transform the current shape into its dual counterpart (made entirely of triangles)
            List<Vector3> dualVertices = new List<Vector3>();
            List<int> dualTriangles = new List<int>();
            for (int j = 0; j < vertices.Count; j++) // for each vertex of the original shape
            {
                List<int> adjacentFaces = new List<int>();
                List<int> previousPoints = new List<int>();
                List<int> nextPoints = new List<int>();
                for (int k = 0; k < pentagons.Count; k++) // for each pentagon of the original shape
                {
                    if (j == pentagons[k]) // if the current pentagon contains the current point
                    {
                        adjacentFaces.Add(k / 5);
                        previousPoints.Add(pentagons[k % 5 == 0 ? k + 4 : k - 1]);
                        nextPoints.Add(pentagons[k % 5 < 4 ? k + 1 : k - 4]);
                    }
                }
                for (int k = 0; k < hexagons.Count; k++) // for each hexagon of the original shape
                {
                    if (j == hexagons[k]) // if the current hexagon contains the current point
                    {
                        adjacentFaces.Add(pentagons.Count / 5 + k / 6);
                        previousPoints.Add(hexagons[k % 6 == 0 ? k + 5 : k - 1]);
                        nextPoints.Add(hexagons[k % 6 < 5 ? k + 1 : k - 5]);
                    }
                }
                if (previousPoints[0] == nextPoints[1] && previousPoints[1] == nextPoints[2] && previousPoints[2] == nextPoints[0]) // if the 3 new vertices were found in clockwise order
                {
                    dualTriangles.Add(adjacentFaces[0]);
                    dualTriangles.Add(adjacentFaces[1]);
                    dualTriangles.Add(adjacentFaces[2]);
                }
                else if (previousPoints[0] == nextPoints[2] && previousPoints[1] == nextPoints[0] && previousPoints[2] == nextPoints[1]) // if the 3 new vertices were found in anticlockwise order
                {
                    dualTriangles.Add(adjacentFaces[0]);
                    dualTriangles.Add(adjacentFaces[2]);
                    dualTriangles.Add(adjacentFaces[1]);
                }
            }
            Vector3 newPoint = new Vector3(0, 0, 0);
            for (int j = 0; j < pentagons.Count; j++) // add a new point at the centre of each pentagon of the original shape and project it onto the unit sphere
            {
                newPoint += vertices[pentagons[j]] / 5;
                if (j % 5 == 4)
                {
                    dualVertices.Add(newPoint.normalized);
                    newPoint = new Vector3(0, 0, 0);
                }
            }
            for (int j = 0; j < hexagons.Count; j++) // add a new point at the centre of each hexagon of the original shape and project it onto the unit sphere
            {
                newPoint += vertices[hexagons[j]] / 6;
                if (j % 6 == 5)
                {
                    dualVertices.Add(newPoint.normalized);
                    newPoint = new Vector3(0, 0, 0);
                }
            }
            List<Vector3> truncatedVertices = new List<Vector3>();
            List<int> truncatedPentagons = new List<int>();
            List<int> truncatedHexagons = new List<int>();
            List<int> dualEdges = new List<int>();
            List<int> truncatedEdges = new List<int>();
            for (int j = 0; j < dualVertices.Count; j++) // for each vertex of the dual shape
            {
                List<int> adjacentFaces = new List<int>();
                List<int> previousPoints = new List<int>();
                List<int> nextPoints = new List<int>();
                for (int k = 0; k < dualTriangles.Count; k++) // for each triangle of the dual shape
                {
                    if (j == dualTriangles[k]) // if the current triangle contains the current point
                    {
                        adjacentFaces.Add(k / 3);
                        previousPoints.Add(dualTriangles[k % 3 == 0 ? k + 2 : k - 1]);
                        nextPoints.Add(dualTriangles[k % 3 < 2 ? k + 1 : k - 2]);
                    }
                }
                int currentIndex = 0;
                do // find next vertex in cycle
                {
                    bool foundNext = false;
                    for (int k = 0; k < adjacentFaces.Count; k++)
                    {
                        if (!foundNext && k != currentIndex && previousPoints[currentIndex] == nextPoints[k])
                        {
                            dualEdges.Add(j);
                            dualEdges.Add(nextPoints[currentIndex]);
                            truncatedEdges.Add(truncatedVertices.Count);
                            if (adjacentFaces.Count == 5) truncatedPentagons.Add(truncatedVertices.Count);
                            else if (adjacentFaces.Count == 6) truncatedHexagons.Add(truncatedVertices.Count);
                            truncatedVertices.Add((dualVertices[j] + (dualVertices[nextPoints[currentIndex]] - dualVertices[j]) / 3).normalized); // add new vertex a third of the way along the edge between the two vertices and project it onto the unit sphere
                            currentIndex = k;
                            foundNext = true;
                        }
                    }
                }
                while (currentIndex != 0);
            }
            for (int j = 0; j < dualTriangles.Count; j++) // for each triangle of the dual shape
            {
                List<int> adjacentDualVertices = new List<int>();
                List<int> adjacentTruncatedVertices = new List<int>();
                for (int k = 0; k < dualEdges.Count; k += 2)
                {
                    if (dualTriangles[j] == dualEdges[k])
                    {
                        adjacentDualVertices.Add(dualEdges[k + 1]);
                        adjacentTruncatedVertices.Add(truncatedEdges[k / 2]);
                    }
                }
                for (int k = 0; k < adjacentDualVertices.Count; k++)
                {
                    if (dualTriangles[j % 3 < 2 ? j + 1 : j - 2] == adjacentDualVertices[k] && dualTriangles[j % 3 == 0 ? j + 2 : j - 1] == adjacentDualVertices[(k + 1) % adjacentDualVertices.Count]) // add these two vertices to current hexagon
                    {
                        truncatedHexagons.Add(adjacentTruncatedVertices[(k + 1) % adjacentTruncatedVertices.Count]);
                        truncatedHexagons.Add(adjacentTruncatedVertices[k]);
                    }
                }
            }
            // overwrite data with that of truncated shape
            vertices = truncatedVertices;
            pentagons = truncatedPentagons;
            hexagons = truncatedHexagons;
        }
        List<int> triangles = new List<int>();
        Vector3 newVertex = new Vector3(0, 0, 0);
        for (int i = 0; i < pentagons.Count; i++) // place new vertex in the centre of each pentagonal face and connect it to the edges with triangles
        {
            newVertex += vertices[pentagons[i]] / 5;
            triangles.Add(pentagons[i]);
            if (i % 5 < 4)
            {
                triangles.Add(pentagons[i + 1]);
                triangles.Add(vertices.Count);
            }
            else
            {
                triangles.Add(pentagons[i - 4]);
                triangles.Add(vertices.Count);
                vertices.Add(newVertex);
                newVertex = new Vector3(0, 0, 0);
            }
        }
        for (int i = 0; i < hexagons.Count; i++) // place new vertex in the centre of each hexagonal face and connect it to the edges with triangles
        {
            newVertex += vertices[hexagons[i]] / 6;
            triangles.Add(hexagons[i]);
            if (i % 6 < 5)
            {
                triangles.Add(hexagons[i + 1]);
                triangles.Add(vertices.Count);
            }
            else
            {
                triangles.Add(hexagons[i - 5]);
                triangles.Add(vertices.Count);
                vertices.Add(newVertex);
                newVertex = new Vector3(0, 0, 0);
            }
        }
        return new Mesh()
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray()
        };
    }
}
