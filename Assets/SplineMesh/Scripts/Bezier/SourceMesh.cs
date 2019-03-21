﻿using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace SplineMesh {
    public class SourceMesh {
        private Vector3 translation;
        private Quaternion rotation;
        private Vector3 scale = Vector3.one;

        private readonly Mesh mesh;
        internal Mesh Mesh {
            get {
                return mesh;
            }
        }

        private List<MeshVertex> vertices;
        internal List<MeshVertex> Vertices {
            get {
                if (vertices == null) BuildData();
                return vertices;
            }
        }

        private int[] triangles;
        internal int[] Triangles {
            get {
                if (vertices == null) BuildData();
                return triangles;
            }
        }

        private float minX;
        internal float MinX {
            get {
                if (vertices == null) BuildData();
                return minX;
            }
        }

        private float length;
        internal float Length {
            get {
                if (vertices == null) BuildData();
                return minX;
            }
        }

        /// <summary>
        /// constructor is private to enable fluent builder pattern.
        /// Use <see cref="Build(Mesh)"/> to obtain an instance.
        /// </summary>
        /// <param name="mesh"></param>
        private SourceMesh(Mesh mesh) {
            this.mesh = mesh;
        }

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="other"></param>
        private SourceMesh(SourceMesh other) {
            mesh = other.mesh;
            translation = other.translation;
            rotation = other.rotation;
            scale = other.scale;
        }

        public static SourceMesh Build(Mesh mesh) {
            return new SourceMesh(mesh);
        }

        public SourceMesh Translate(Vector3 translation) {
            var res = new SourceMesh(this) {
                translation = translation
            };
            return res;
        }

        public SourceMesh Rotate(Quaternion rotation) {
            var res = new SourceMesh(this) {
                rotation = rotation
            };
            return res;
        }

        public SourceMesh Scale(Vector3 scale) {
            var res = new SourceMesh(this) {
                scale = scale
            };
            return res;
        }

        /// <summary>
        /// Build data that are consistent between computations if no property has been changed.
        /// This method allows the computation due to curve changes to be faster.
        /// </summary>
        private void BuildData() {
            var baseVertices = new List<MeshVertex>();
            int i = 0;
            foreach (Vector3 vert in mesh.vertices) {
                var v = new MeshVertex(vert, mesh.normals[i++], 0);
                baseVertices.Add(v);
            }


            // if the mesh is reversed by scale, we must change the culling of the faces by inversing all triangles.
            // the mesh is reverse only if the number of resersing axes is impair.
            bool reversed = scale.x < 0;
            if (scale.y < 0) reversed = !reversed;
            if (scale.z < 0) reversed = !reversed;
            triangles = reversed ? MeshUtility.GetReversedTriangles(mesh) : mesh.triangles;

            // we transform the source mesh vertices according to rotation/translation/scale
            vertices.Clear();
            foreach (var vert in baseVertices) {
                var transformed = new MeshVertex(vert.v, vert.n, 0);
                //  application of rotation
                if (rotation != Quaternion.identity) {
                    transformed.v = rotation * transformed.v;
                    transformed.n = rotation * transformed.n;
                }
                if (scale != Vector3.one) {
                    transformed.v = Vector3.Scale(transformed.v, scale);
                    transformed.n = Vector3.Scale(transformed.n, scale);
                }
                if (translation != Vector3.zero) {
                    transformed.v += translation;
                }
                vertices.Add(transformed);
            }

            // find the bounds along x
            minX = float.MaxValue;
            float maxX = float.MinValue;
            foreach (Vertex vert in vertices) {
                Vector3 p = vert.v;
                maxX = Math.Max(maxX, p.x);
                minX = Math.Min(minX, p.x);
            }
            length = Math.Abs(maxX - minX);
        }
    }
}
