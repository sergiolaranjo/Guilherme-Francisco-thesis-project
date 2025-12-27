// ============================================================================
// CardiacVR - VR Platform for Cardiac Surgery Planning
// Copyright (c) 2024 Sérgio Laranjo
// Computational Cardiology, AI and Data Science for Health Lab
// Nova Medical School, Universidade Nova de Lisboa
// All rights reserved.
// ============================================================================

using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Utility class for importing STL (Stereolithography) files.
/// Supports both ASCII and Binary STL formats.
/// </summary>
public static class STLImporter
{
    /// <summary>
    /// Represents a single triangle in an STL file.
    /// </summary>
    public struct STLTriangle
    {
        public Vector3 Normal;
        public Vector3 Vertex1;
        public Vector3 Vertex2;
        public Vector3 Vertex3;
    }

    /// <summary>
    /// Result of an STL import operation.
    /// </summary>
    public class STLImportResult
    {
        public bool Success;
        public string ErrorMessage;
        public Mesh Mesh;
        public string Name;
        public int TriangleCount;
        public Bounds Bounds;
    }

    /// <summary>
    /// Import an STL file and create a Unity Mesh.
    /// Automatically detects ASCII or Binary format.
    /// </summary>
    public static STLImportResult Import(string filePath)
    {
        STLImportResult result = new STLImportResult();

        if (!File.Exists(filePath))
        {
            result.Success = false;
            result.ErrorMessage = $"File not found: {filePath}";
            return result;
        }

        try
        {
            byte[] fileData = File.ReadAllBytes(filePath);

            if (IsBinarySTL(fileData))
            {
                return ImportBinary(fileData, Path.GetFileNameWithoutExtension(filePath));
            }
            else
            {
                string fileContent = Encoding.ASCII.GetString(fileData);
                return ImportASCII(fileContent, Path.GetFileNameWithoutExtension(filePath));
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Failed to import STL: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Import an STL file from a byte array.
    /// </summary>
    public static STLImportResult Import(byte[] data, string name = "STLMesh")
    {
        if (data == null || data.Length == 0)
        {
            return new STLImportResult
            {
                Success = false,
                ErrorMessage = "Data is null or empty"
            };
        }

        try
        {
            if (IsBinarySTL(data))
            {
                return ImportBinary(data, name);
            }
            else
            {
                string content = Encoding.ASCII.GetString(data);
                return ImportASCII(content, name);
            }
        }
        catch (Exception ex)
        {
            return new STLImportResult
            {
                Success = false,
                ErrorMessage = $"Failed to import STL: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Check if STL data is in binary format.
    /// </summary>
    private static bool IsBinarySTL(byte[] data)
    {
        // Binary STL files have an 80-byte header followed by a 4-byte triangle count
        // ASCII STL files start with "solid"

        if (data.Length < 84)
        {
            // Too small to be binary, try ASCII
            return false;
        }

        // Check if it starts with "solid" (ASCII format indicator)
        string header = Encoding.ASCII.GetString(data, 0, Math.Min(80, data.Length));
        if (header.TrimStart().StartsWith("solid", StringComparison.OrdinalIgnoreCase))
        {
            // Could be ASCII, but some binary files also start with "solid"
            // Check if the rest looks like ASCII
            string sampleContent = Encoding.ASCII.GetString(data, 0, Math.Min(1000, data.Length));
            if (sampleContent.Contains("facet normal") && sampleContent.Contains("vertex"))
            {
                return false; // ASCII format
            }
        }

        // Check if triangle count makes sense for binary
        uint triangleCount = BitConverter.ToUInt32(data, 80);
        uint expectedSize = 84 + triangleCount * 50; // header + count + (normal + 3 vertices + attribute) per triangle

        // Allow some tolerance for file size
        return Math.Abs((long)data.Length - (long)expectedSize) < 100;
    }

    /// <summary>
    /// Import Binary STL format.
    /// </summary>
    private static STLImportResult ImportBinary(byte[] data, string name)
    {
        STLImportResult result = new STLImportResult { Name = name };

        try
        {
            // Skip 80-byte header
            int offset = 80;

            // Read triangle count (4 bytes, little-endian)
            uint triangleCount = BitConverter.ToUInt32(data, offset);
            offset += 4;

            result.TriangleCount = (int)triangleCount;

            if (triangleCount == 0)
            {
                result.Success = false;
                result.ErrorMessage = "STL file contains no triangles";
                return result;
            }

            // Limit for Unity mesh (65535 vertices per submesh, but we use 32-bit indices)
            if (triangleCount > 10000000)
            {
                result.Success = false;
                result.ErrorMessage = $"STL file too large: {triangleCount} triangles";
                return result;
            }

            List<Vector3> vertices = new List<Vector3>((int)triangleCount * 3);
            List<Vector3> normals = new List<Vector3>((int)triangleCount * 3);
            List<int> triangles = new List<int>((int)triangleCount * 3);

            for (uint i = 0; i < triangleCount; i++)
            {
                // Read normal (3 floats = 12 bytes)
                Vector3 normal = new Vector3(
                    BitConverter.ToSingle(data, offset),
                    BitConverter.ToSingle(data, offset + 4),
                    BitConverter.ToSingle(data, offset + 8)
                );
                offset += 12;

                // Read 3 vertices (each 3 floats = 12 bytes)
                Vector3 v1 = new Vector3(
                    BitConverter.ToSingle(data, offset),
                    BitConverter.ToSingle(data, offset + 4),
                    BitConverter.ToSingle(data, offset + 8)
                );
                offset += 12;

                Vector3 v2 = new Vector3(
                    BitConverter.ToSingle(data, offset),
                    BitConverter.ToSingle(data, offset + 4),
                    BitConverter.ToSingle(data, offset + 8)
                );
                offset += 12;

                Vector3 v3 = new Vector3(
                    BitConverter.ToSingle(data, offset),
                    BitConverter.ToSingle(data, offset + 4),
                    BitConverter.ToSingle(data, offset + 8)
                );
                offset += 12;

                // Skip attribute byte count (2 bytes)
                offset += 2;

                // Convert from STL coordinate system (Z-up) to Unity (Y-up)
                v1 = ConvertCoordinates(v1);
                v2 = ConvertCoordinates(v2);
                v3 = ConvertCoordinates(v3);
                normal = ConvertCoordinates(normal);

                // Add vertices and indices
                int baseIndex = vertices.Count;
                vertices.Add(v1);
                vertices.Add(v2);
                vertices.Add(v3);

                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);

                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);
            }

            result.Mesh = CreateMesh(vertices, normals, triangles, name);
            result.Bounds = result.Mesh.bounds;
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Failed to parse binary STL: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Import ASCII STL format.
    /// </summary>
    private static STLImportResult ImportASCII(string content, string name)
    {
        STLImportResult result = new STLImportResult { Name = name };

        try
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<int> triangles = new List<int>();

            string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            Vector3 currentNormal = Vector3.zero;
            List<Vector3> currentVertices = new List<Vector3>();
            int triangleCount = 0;

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim().ToLowerInvariant();

                if (line.StartsWith("facet normal"))
                {
                    currentNormal = ParseVector3(line.Substring(12));
                    currentNormal = ConvertCoordinates(currentNormal);
                }
                else if (line.StartsWith("vertex"))
                {
                    Vector3 vertex = ParseVector3(line.Substring(6));
                    vertex = ConvertCoordinates(vertex);
                    currentVertices.Add(vertex);
                }
                else if (line.StartsWith("endfacet"))
                {
                    if (currentVertices.Count >= 3)
                    {
                        int baseIndex = vertices.Count;

                        vertices.Add(currentVertices[0]);
                        vertices.Add(currentVertices[1]);
                        vertices.Add(currentVertices[2]);

                        normals.Add(currentNormal);
                        normals.Add(currentNormal);
                        normals.Add(currentNormal);

                        triangles.Add(baseIndex);
                        triangles.Add(baseIndex + 1);
                        triangles.Add(baseIndex + 2);

                        triangleCount++;
                    }
                    currentVertices.Clear();
                }
            }

            if (triangleCount == 0)
            {
                result.Success = false;
                result.ErrorMessage = "No triangles found in ASCII STL file";
                return result;
            }

            result.TriangleCount = triangleCount;
            result.Mesh = CreateMesh(vertices, normals, triangles, name);
            result.Bounds = result.Mesh.bounds;
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Failed to parse ASCII STL: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Parse a Vector3 from a space-separated string.
    /// </summary>
    private static Vector3 ParseVector3(string text)
    {
        string[] parts = text.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 3)
        {
            throw new FormatException($"Invalid vector format: {text}");
        }

        return new Vector3(
            float.Parse(parts[0], CultureInfo.InvariantCulture),
            float.Parse(parts[1], CultureInfo.InvariantCulture),
            float.Parse(parts[2], CultureInfo.InvariantCulture)
        );
    }

    /// <summary>
    /// Convert from STL coordinate system (Z-up, right-handed) to Unity (Y-up, left-handed).
    /// </summary>
    private static Vector3 ConvertCoordinates(Vector3 v)
    {
        // Swap Y and Z, negate new Z for handedness
        return new Vector3(v.x, v.z, v.y);
    }

    /// <summary>
    /// Create a Unity Mesh from vertices, normals, and triangles.
    /// </summary>
    private static Mesh CreateMesh(List<Vector3> vertices, List<Vector3> normals, List<int> triangles, string name)
    {
        Mesh mesh = new Mesh();
        mesh.name = name;

        // Use 32-bit indices for large meshes
        mesh.indexFormat = vertices.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);

        mesh.RecalculateBounds();

        // Recalculate normals if they seem invalid
        bool hasInvalidNormals = false;
        foreach (Vector3 normal in normals)
        {
            if (normal.sqrMagnitude < 0.01f)
            {
                hasInvalidNormals = true;
                break;
            }
        }

        if (hasInvalidNormals)
        {
            mesh.RecalculateNormals();
        }

        mesh.RecalculateTangents();

        return mesh;
    }

    /// <summary>
    /// Optimize a mesh by welding duplicate vertices.
    /// </summary>
    public static Mesh OptimizeMesh(Mesh originalMesh, float weldThreshold = 0.0001f)
    {
        if (originalMesh == null) return null;

        Vector3[] originalVertices = originalMesh.vertices;
        Vector3[] originalNormals = originalMesh.normals;
        int[] originalTriangles = originalMesh.triangles;

        Dictionary<Vector3, int> uniqueVertices = new Dictionary<Vector3, int>(new Vector3Comparer(weldThreshold));
        List<Vector3> newVertices = new List<Vector3>();
        List<Vector3> newNormals = new List<Vector3>();
        int[] vertexMap = new int[originalVertices.Length];

        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 vertex = originalVertices[i];

            if (uniqueVertices.TryGetValue(vertex, out int existingIndex))
            {
                vertexMap[i] = existingIndex;
                // Average normals for welded vertices
                newNormals[existingIndex] = (newNormals[existingIndex] + originalNormals[i]).normalized;
            }
            else
            {
                int newIndex = newVertices.Count;
                uniqueVertices[vertex] = newIndex;
                vertexMap[i] = newIndex;
                newVertices.Add(vertex);
                newNormals.Add(originalNormals[i]);
            }
        }

        int[] newTriangles = new int[originalTriangles.Length];
        for (int i = 0; i < originalTriangles.Length; i++)
        {
            newTriangles[i] = vertexMap[originalTriangles[i]];
        }

        Mesh optimizedMesh = new Mesh();
        optimizedMesh.name = originalMesh.name + "_optimized";
        optimizedMesh.indexFormat = newVertices.Count > 65535
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;

        optimizedMesh.SetVertices(newVertices);
        optimizedMesh.SetNormals(newNormals);
        optimizedMesh.SetTriangles(newTriangles, 0);
        optimizedMesh.RecalculateBounds();
        optimizedMesh.RecalculateTangents();

        Debug.Log($"Mesh optimized: {originalVertices.Length} -> {newVertices.Count} vertices");

        return optimizedMesh;
    }

    /// <summary>
    /// Custom comparer for Vector3 with threshold-based equality.
    /// </summary>
    private class Vector3Comparer : IEqualityComparer<Vector3>
    {
        private readonly float threshold;

        public Vector3Comparer(float threshold)
        {
            this.threshold = threshold;
        }

        public bool Equals(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b) < threshold;
        }

        public int GetHashCode(Vector3 v)
        {
            // Round to grid based on threshold for hash consistency
            float gridSize = threshold * 10;
            int x = Mathf.RoundToInt(v.x / gridSize);
            int y = Mathf.RoundToInt(v.y / gridSize);
            int z = Mathf.RoundToInt(v.z / gridSize);
            return x.GetHashCode() ^ (y.GetHashCode() << 8) ^ (z.GetHashCode() << 16);
        }
    }
}
