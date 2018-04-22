﻿using Csgo.Bsp;
using Csgo.Bsp.LumpData;
using Csgo.Util;
using Csgo.Vtf;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Csgo.MapLoader
{
    public class BrushModel: Model
    {
        private readonly Map map;
        private readonly Materials materials;

        private readonly Dictionary<string, List<uint>> indicesByTexture = new Dictionary<string, List<uint>>();
        private readonly List<Vertex> vertices = new List<Vertex>();

        public BrushModel(Map map, Materials materials)
        {
            this.map = map;
            this.materials = materials;
            var rootNode = map.Lumps.GetNodes()[0];
            BuildBuffers(rootNode);

            foreach (var (materialName, indices) in indicesByTexture)
            {
                var indexPositions = new Dictionary<uint, uint>();
                var meshIndices = new List<uint>();
                var meshVertices = new List<Vertex>();

                for (var i = 0; i < indices.Count; i++)
                {
                    if (indexPositions.TryGetValue(indices[i], out uint index))
                    {
                        meshIndices.Add(index);
                    }
                    else
                    {
                        index = (uint)(meshVertices.Count);
                        meshVertices.Add(vertices[(int)indices[i]]);
                        meshIndices.Add(index);
                        indexPositions[indices[i]] = index;
                    }
                }

                if (meshIndices.Count % 3 != 0)
                {
                    throw new Exception("Broken triangles");
                }
                this.Meshes.Add((materials[materialName], new Mesh {
                    Indices = meshIndices.ToArray(),
                    Vertices = meshVertices.ToArray()
                }));
            }
        }

        private void BuildBuffers(Leaf leaf)
        {
            var bspVertices = map.Lumps.GetVertices();

            var leafFaces = map.Lumps.GetLeafFaces();
            var faces = map.Lumps.GetFaces();
            var edges = map.Lumps.GetEdges();
            var surfEdges = map.Lumps.GetSurfaceEdges();
            var normals = map.Lumps.GetVertexNormals();
            var normalIndices = map.Lumps.GetVertexNormalIndices();
            for (var leafFaceIndex = leaf.FirstLeafFace; leafFaceIndex < leaf.FirstLeafFace + leaf.LeafFacesCount; leafFaceIndex++)
            {
                var faceIndex = leafFaces[leafFaceIndex];
                var face = faces[faceIndex];
                var texInfo = map.Lumps.GetTextureInfo()[face.TextureInfo];
                var texData = map.Lumps.GetTextureData()[texInfo.TextureData];
                var textureOffset = map.Lumps.GetTextureDataStringTable()[texData.NameStringTableId];
                var textureName = map.Lumps.GetTextureDataString()[textureOffset];
                uint rootVertex = 0;

                //var normal = normals[normalIndices[faceIndex]];

                SourceMaterial material = materials[textureName];
                SourceTexture texture = materials.LoadTexture(material?.BaseTextureName);

                float texWidth = texData.Width;
                float texHeight = texData.Height;
                if (texture != null)
                {
                    if (texture.Width != texData.Width || texture.Height != texData.Height)
                    {
                        texWidth = texture.Width;
                        texHeight = texture.Height;
                    }
                }

                var faceVertices = new Dictionary<uint, uint>();

                for (var surfEdgeIndex = face.FirstEdge; surfEdgeIndex < face.FirstEdge + face.EdgesCount; surfEdgeIndex++)
                {
                    var edgeIndex = surfEdges[surfEdgeIndex];
                    var edge = edges[Math.Abs(edgeIndex)];

                    if (surfEdgeIndex == face.FirstEdge)
                    {
                        if (!faceVertices.ContainsKey(edge.VertexIndex[edgeIndex > 0 ? 0 : 1]))
                        {
                            rootVertex = edge.VertexIndex[edgeIndex > 0 ? 0 : 1];
                            var vertex = new Vertex();
                            vertex.Position = bspVertices[rootVertex];
                            //vertex.Normal = normal;
                            if (texWidth != 0 && texHeight != 0)
                            {
                                vertex.TextureCoord = new Vector2
                                {
                                    X = Vector4.Dot(new Vector4(vertex.Position, 1), texInfo.TextureVecsS) / texWidth,
                                    Y = Vector4.Dot(new Vector4(vertex.Position, 1), texInfo.TextureVecsT) / texHeight,
                                };
                            }
                            else
                            {
                                vertex.TextureCoord = new Vector2();
                            }
                            faceVertices.Add(rootVertex, (uint)vertices.Count);
                            vertices.Add(vertex);
                        }
                        continue;
                    }

                    //Edge must not be connected to the root vertex
                    if (edge.VertexIndex[0] == rootVertex || edge.VertexIndex[1] == rootVertex)
                    {
                        continue;
                    }

                    for (var i = 0; i < 2; i++)
                    {
                        if (!faceVertices.ContainsKey(edge.VertexIndex[i]))
                        {
                            var vertex = new Vertex();
                            vertex.Position = bspVertices[edge.VertexIndex[i]];
                            //vertex.Normal = normal;
                            if (texWidth != 0 && texHeight != 0)
                            {
                                vertex.TextureCoord = new Vector2
                                {
                                    X = Vector4.Dot(new Vector4(vertex.Position, 1), texInfo.TextureVecsS) / texWidth,
                                    Y = Vector4.Dot(new Vector4(vertex.Position, 1), texInfo.TextureVecsT) / texHeight,
                                };
                            }
                            else
                            {
                                vertex.TextureCoord = new Vector2();
                            }
                            vertex.LightmapTextureCoord = new Vector2
                            {
                                X = (Vector3.Dot(vertex.Position, texInfo.LightmapVecsS.Xyz()) + texInfo.LightmapVecsS.W) / texData.Width,
                                Y = (Vector3.Dot(vertex.Position, texInfo.LightmapVecsT.Xyz()) + texInfo.LightmapVecsT.W) / texData.Height,
                            };
                            faceVertices.Add(edge.VertexIndex[i], (uint)vertices.Count);
                            vertices.Add(vertex);
                        }
                    }

                    List<uint> indices;
                    if (!indicesByTexture.TryGetValue(textureName, out indices))
                    {
                        indices = new List<uint>();
                        indicesByTexture[textureName] = indices;
                    }

                    indices.Add(faceVertices[rootVertex]);
                    if (edgeIndex < 0)
                    {
                        indices.Add(faceVertices[edge.VertexIndex[1]]);
                        indices.Add(faceVertices[edge.VertexIndex[0]]);
                    }
                    else
                    {
                        indices.Add(faceVertices[edge.VertexIndex[0]]);
                        indices.Add(faceVertices[edge.VertexIndex[1]]);
                    }
                }
            }
            CalculateNormals();
        }

        private void CalculateNormals()
        {
            List<uint> indices = new List<uint>();
            foreach (var (_, textureIndices) in indicesByTexture)
            {
                foreach (var index in textureIndices)
                {
                    indices.Add(index);
                }
            }
            for (var i = 0; i < indices.Count - 2; i += 3)
            {
                var vertex0 = vertices[(int)indices[i]];
                var vertex1 = vertices[(int)indices[i + 1]];
                var vertex2 = vertices[(int)indices[i + 2]];

                var vec1 = vertex0.Position - vertex2.Position;
                var vec2 = vertex1.Position - vertex2.Position;
                var cross = Vector3.Cross(vec1, vec2);
                var surfaceNormal = Vector3.Normalize(cross);

                for (int ii = 0; ii < 3; ii++)
                {
                    var vertex = vertices[(int)indices[i + ii]];
                    vertex.Normal = surfaceNormal;
                    vertices[(int)indices[i + ii]] = vertex;
                }
            }
        }

        private void BuildBuffers(Node node)
        {
            var nodes = map.Lumps.GetNodes();
            var leafs = map.Lumps.GetLeafs();
            var leftChild = node.Children[0];
            BuildChild(leftChild);
            var rightChild = node.Children[1];
            BuildChild(rightChild);
        }

        private void BuildChild(int childIndex)
        {
            var nodes = map.Lumps.GetNodes();
            var leafs = map.Lumps.GetLeafs();
            if (childIndex < 0)
            {
                BuildBuffers(leafs[-1 - childIndex]);
            }
            else
            {
                BuildBuffers(nodes[childIndex]);
            }
        }
    }
}
