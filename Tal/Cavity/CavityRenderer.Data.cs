// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Cavity detection / analysis module: CavityRenderer.Data.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.CompilerServices;
using SysVector3 = System.Numerics.Vector3;
using Color = Microsoft.Xna.Framework.Color;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;
using XnaVector4 = Microsoft.Xna.Framework.Vector4;

namespace Tal.Cavity
{
    public partial class CavityRenderer
    {
        #region Nested Classes

        public class RenderCavity
        {
            public XnaVector3[] BaseVoxels { get; set; }
            public XnaVector3 BaseCenter { get; set; }
            public BoundingBox BaseBoundingBox { get; set; }
            public XnaVector4 BaseColor4 { get; set; } = XnaVector4.One;
            public XnaVector3 CenterOffset { get; set; } = XnaVector3.Zero;
            public float Volume { get; set; }
            public bool IsExterior { get; set; } = false;
            public float[,,] DensityGrid { get; set; }
            public int DensityNx { get; set; }
            public int DensityNy { get; set; }
            public int DensityNz { get; set; }
            public float DensityStepX { get; set; }
            public float DensityStepY { get; set; }
            public float DensityStepZ { get; set; }
            public MeshDataXna RawMeshForSurface { get; set; }
            public MeshDataXna MarchingCubesMesh { get; set; }
            public bool MarchingCubesGenerated { get; set; } = false;
            public XnaVector3[] CachedNormals { get; set; }
            public bool NormalsCached { get; set; } = false;
            public MeshDataXna BaseMarchingCubesMesh { get; set; }
            public XnaVector4[] VertexColors4 { get; set; } = null;
            public bool HasVertexColors => VertexColors4 != null && VertexColors4.Length > 0;

            public int CavityId { get; set; }
            public bool Visible { get; set; } = true;
            public int TopologyType { get; set; }
            public int WallConnections { get; set; }
            public HashSet<int> ConnectedCavities { get; set; } = new HashSet<int>();
            public bool IsPeriodic { get; set; }
            public bool[,,] VoxelMask { get; set; }
            public int WallXMin { get; set; } = 0;
            public int WallXMax { get; set; } = 0;
            public int WallYMin { get; set; } = 0;
            public int WallYMax { get; set; } = 0;
            public int WallZMin { get; set; } = 0;
            public int WallZMax { get; set; } = 0;
            public int PeriodicGroupId { get; set; } = -1;
            public bool IsPeriodicDuplicate { get; set; } = false;
            public bool TouchesXMin { get; set; } = false;
            public bool TouchesXMax { get; set; } = false;
            public bool TouchesYMin { get; set; } = false;
            public bool TouchesYMax { get; set; } = false;
            public bool TouchesZMin { get; set; } = false;
            public bool TouchesZMax { get; set; } = false;

            public Dictionary<string, ChannelPathResultXna> BaseCrystalChannelResults { get; set; }
                = new Dictionary<string, ChannelPathResultXna>();
            public Dictionary<string, (List<XnaVector3> MinPath, List<XnaVector3> MaxPath)> BaseCrystalChannelBothPaths { get; set; }
                = new Dictionary<string, (List<XnaVector3>, List<XnaVector3>)>();

            public Dictionary<string, ChannelPathResultXna> CrystalChannelResults { get; set; }
                = new Dictionary<string, ChannelPathResultXna>();
            public Dictionary<string, (List<XnaVector3> MinPath, List<XnaVector3> MaxPath)> CrystalChannelBothPaths { get; set; }
                = new Dictionary<string, (List<XnaVector3>, List<XnaVector3>)>();

            public float MaxSphereRadius { get; set; } = 0f;
            public SysVector3 SphereCenter { get; set; } = SysVector3.Zero;
            public bool SphereFitCalculated { get; set; } = false;
            public List<SphereFitAnalyzer.SphereFitResult> SphereFits { get; set; } = new List<SphereFitAnalyzer.SphereFitResult>();
            public bool HasMultipleSpheres => SphereFits != null && SphereFits.Count > 0;

            public RenderCavity(Cavity source)
            {
                BaseVoxels = source.Voxels != null && source.Voxels.Count > 0
                    ? source.Voxels.Select(v => new XnaVector3(v.X, v.Y, v.Z)).ToArray()
                    : Array.Empty<XnaVector3>();

                BaseCenter = new XnaVector3(source.Center.X, source.Center.Y, source.Center.Z);
                BaseBoundingBox = source.BoundingBox;
                BaseColor4 = source.BaseColor4;
                Volume = source.Volume;
                IsExterior = source.IsExterior;

                DensityGrid = source.DensityGrid;
                DensityNx = source.DensityNx;
                DensityNy = source.DensityNy;
                DensityNz = source.DensityNz;
                DensityStepX = source.DensityStepX;
                DensityStepY = source.DensityStepY;
                DensityStepZ = source.DensityStepZ;

                CavityId = source.Id;
                Visible = source.Visible;
                TopologyType = source.TopologyType;
                WallConnections = source.WallConnections;
                ConnectedCavities = new HashSet<int>(source.ConnectedCavities);
                IsPeriodic = source.IsPeriodic;
                PeriodicGroupId = source.PeriodicGroupId;
                IsPeriodicDuplicate = source.IsPeriodicDuplicate;

                WallXMin = source.WallXMin;
                WallXMax = source.WallXMax;
                WallYMin = source.WallYMin;
                WallYMax = source.WallYMax;
                WallZMin = source.WallZMin;
                WallZMax = source.WallZMax;

                TouchesXMin = source.TouchesXMin;
                TouchesXMax = source.TouchesXMax;
                TouchesYMin = source.TouchesYMin;
                TouchesYMax = source.TouchesYMax;
                TouchesZMin = source.TouchesZMin;
                TouchesZMax = source.TouchesZMax;

                VoxelMask = source.VoxelMask;
                VertexColors4 = source.VertexColors4;

                if (source.TrianglesGenerated && source.Vertices?.Count > 0 && source.Indices?.Count > 0)
                {
                    var mesh = new MeshDataXna
                    {
                        Vertices = source.Vertices.Select(v => new XnaVector3(v.X, v.Y, v.Z)).ToList(),
                        Indices = new List<int>(source.Indices),
                    };
                    BaseMarchingCubesMesh = mesh;
                    MarchingCubesMesh = mesh.CloneWithShiftXna(XnaVector3.Zero);
                    MarchingCubesGenerated = true;

                    if (source.CrystalChannelResults != null && source.CrystalChannelResults.Count > 0)
                    {
                        foreach (var kvp in source.CrystalChannelResults)
                        {
                            var result = kvp.Value;
                            var worldPathXna = result.WorldPathXna ?? new List<XnaVector3>();

                            var baseResult = new ChannelPathResultXna
                            {
                                IsConnected = result.IsConnected,
                                MinPath = new List<XnaVector3>(worldPathXna),
                                MaxPath = new List<XnaVector3>(worldPathXna),
                                WorldPath = new List<XnaVector3>(worldPathXna)
                            };
                            BaseCrystalChannelResults[kvp.Key] = baseResult;
                            BaseCrystalChannelBothPaths[kvp.Key] = (
                                new List<XnaVector3>(worldPathXna),
                                new List<XnaVector3>(worldPathXna)
                            );

                            CrystalChannelResults[kvp.Key] = baseResult;
                            CrystalChannelBothPaths[kvp.Key] = (
                                new List<XnaVector3>(worldPathXna),
                                new List<XnaVector3>(worldPathXna)
                            );
                        }
                    }
                }
                else
                {
                    MarchingCubesGenerated = false;
                }

                MaxSphereRadius = source.MaxSphereRadius;
                SphereCenter = source.SphereCenter;
                SphereFitCalculated = source.SphereFitCalculated;
                SphereFits = new List<SphereFitAnalyzer.SphereFitResult>(source.SphereFits);
            }

            public RenderCavity()
            {
                BaseVoxels = Array.Empty<XnaVector3>();
                BaseCenter = XnaVector3.Zero;
                BaseBoundingBox = new BoundingBox();
                BaseColor4 = XnaVector4.One;
                Volume = 0;
                IsExterior = false;
                MarchingCubesGenerated = false;
                PeriodicGroupId = -1;
                IsPeriodicDuplicate = false;
                VertexColors4 = null;
                MaxSphereRadius = 0f;
                SphereCenter = SysVector3.Zero;
                SphereFitCalculated = false;
                SphereFits = new List<SphereFitAnalyzer.SphereFitResult>();
            }

            public void ApplyShift(XnaVector3 shift)
            {
                CenterOffset = shift;

                if (MarchingCubesGenerated && BaseMarchingCubesMesh != null)
                {
                    MarchingCubesMesh = BaseMarchingCubesMesh.CloneWithShiftXna(shift);
                    NormalsCached = false;
                    CachedNormals = null;
                }

                if (BaseCrystalChannelBothPaths != null && BaseCrystalChannelBothPaths.Count > 0)
                {
                    var newBothPaths = new Dictionary<string, (List<XnaVector3>, List<XnaVector3>)>();
                    foreach (var kvp in BaseCrystalChannelBothPaths)
                    {
                        var minPath = kvp.Value.MinPath?.Select(p => p + shift).ToList() ?? new List<XnaVector3>();
                        var maxPath = kvp.Value.MaxPath?.Select(p => p + shift).ToList() ?? new List<XnaVector3>();
                        newBothPaths[kvp.Key] = (minPath, maxPath);
                    }
                    CrystalChannelBothPaths = newBothPaths;

                    var newResults = new Dictionary<string, ChannelPathResultXna>();
                    foreach (var kvp in BaseCrystalChannelResults)
                    {
                        var result = kvp.Value;
                        newResults[kvp.Key] = new ChannelPathResultXna
                        {
                            IsConnected = result.IsConnected,
                            MinPath = result.MinPath?.Select(p => p + shift).ToList() ?? new List<XnaVector3>(),
                            MaxPath = result.MaxPath?.Select(p => p + shift).ToList() ?? new List<XnaVector3>(),
                            WorldPath = result.WorldPath?.Select(p => p + shift).ToList() ?? new List<XnaVector3>()
                        };
                    }
                    CrystalChannelResults = newResults;
                }
            }

            public RenderCavity CloneWithShift(XnaVector3 shift)
            {
                var copy = new RenderCavity
                {
                    BaseVoxels = BaseVoxels,
                    BaseCenter = BaseCenter + shift,
                    BaseBoundingBox = new BoundingBox(BaseBoundingBox.Min + shift, BaseBoundingBox.Max + shift),
                    BaseColor4 = BaseColor4,
                    Volume = Volume,
                    IsExterior = IsExterior,
                    DensityGrid = DensityGrid,
                    DensityNx = DensityNx,
                    DensityNy = DensityNy,
                    DensityNz = DensityNz,
                    DensityStepX = DensityStepX,
                    DensityStepY = DensityStepY,
                    DensityStepZ = DensityStepZ,
                    BaseMarchingCubesMesh = BaseMarchingCubesMesh?.CloneWithShiftXna(shift),
                    MarchingCubesMesh = BaseMarchingCubesMesh?.CloneWithShiftXna(shift),
                    MarchingCubesGenerated = MarchingCubesGenerated,
                    CenterOffset = shift,
                    CavityId = CavityId,
                    Visible = Visible,
                    TopologyType = TopologyType,
                    WallConnections = WallConnections,
                    ConnectedCavities = new HashSet<int>(ConnectedCavities),
                    IsPeriodic = IsPeriodic,
                    PeriodicGroupId = PeriodicGroupId,
                    IsPeriodicDuplicate = IsPeriodicDuplicate,
                    WallXMin = WallXMin,
                    WallXMax = WallXMax,
                    WallYMin = WallYMin,
                    WallYMax = WallYMax,
                    WallZMin = WallZMin,
                    WallZMax = WallZMax,
                    TouchesXMin = TouchesXMin,
                    TouchesXMax = TouchesXMax,
                    TouchesYMin = TouchesYMin,
                    TouchesYMax = TouchesYMax,
                    TouchesZMin = TouchesZMin,
                    TouchesZMax = TouchesZMax,
                    VoxelMask = VoxelMask,
                    VertexColors4 = VertexColors4,
                    MaxSphereRadius = MaxSphereRadius,
                    SphereCenter = SphereCenter,
                    SphereFitCalculated = SphereFitCalculated,
                    SphereFits = new List<SphereFitAnalyzer.SphereFitResult>(SphereFits),
                    BaseCrystalChannelResults = new Dictionary<string, ChannelPathResultXna>(),
                    BaseCrystalChannelBothPaths = new Dictionary<string, (List<XnaVector3>, List<XnaVector3>)>(),
                    CrystalChannelResults = new Dictionary<string, ChannelPathResultXna>(),
                    CrystalChannelBothPaths = new Dictionary<string, (List<XnaVector3>, List<XnaVector3>)>()
                };

                foreach (var kvp in BaseCrystalChannelResults)
                {
                    var result = kvp.Value;
                    copy.BaseCrystalChannelResults[kvp.Key] = new ChannelPathResultXna
                    {
                        IsConnected = result.IsConnected,
                        MinPath = new List<XnaVector3>(result.MinPath ?? new List<XnaVector3>()),
                        MaxPath = new List<XnaVector3>(result.MaxPath ?? new List<XnaVector3>()),
                        WorldPath = new List<XnaVector3>(result.WorldPath ?? new List<XnaVector3>())
                    };
                }
                foreach (var kvp in BaseCrystalChannelBothPaths)
                {
                    copy.BaseCrystalChannelBothPaths[kvp.Key] = (
                        new List<XnaVector3>(kvp.Value.MinPath ?? new List<XnaVector3>()),
                        new List<XnaVector3>(kvp.Value.MaxPath ?? new List<XnaVector3>())
                    );
                }

                copy.ApplyShift(shift);
                return copy;
            }
        }

        public class RenderMolecularSurface
        {
            public List<XnaVector3> BaseVertices { get; set; } = new List<XnaVector3>();
            public List<int> BaseIndices { get; set; } = new List<int>();
            public XnaVector4 BaseColor4 { get; set; } = new XnaVector4(0.3f, 0.5f, 0.9f, 0.8f);
            public XnaVector4[] VertexColors4 { get; set; } = null;
            public bool HasVertexColors => VertexColors4 != null && VertexColors4.Length > 0;

            public List<XnaVector3> Vertices { get; set; } = new List<XnaVector3>();
            public List<int> Indices { get; set; } = new List<int>();
            public XnaVector3 CenterOffset { get; set; } = XnaVector3.Zero;

            public XnaVector3[] CachedNormals { get; set; }
            public bool NormalsCached { get; set; } = false;

            public int MoleculeId { get; set; }
            public int AtomCount { get; set; }
            public float SurfaceArea { get; set; }
            public float Volume { get; set; }
            public List<RedSpot> RedSpots { get; set; } = new List<RedSpot>();
            public List<SurfacePoint> SurfacePoints { get; set; } = new List<SurfacePoint>();

            public bool IsValid => Vertices != null && Vertices.Count > 0 && Indices != null && Indices.Count > 0;

            public RenderMolecularSurface(MolecularSurface source)
            {
                MoleculeId = source.MoleculeId;
                AtomCount = source.AtomCount;
                SurfaceArea = source.SurfaceArea;
                Volume = source.Volume;
                RedSpots = new List<RedSpot>(source.RedSpots ?? new List<RedSpot>());
                SurfacePoints = new List<SurfacePoint>(source.SurfacePoints ?? new List<SurfacePoint>());

                BaseVertices = source.Vertices.Select(v => new XnaVector3(v.X, v.Y, v.Z)).ToList();
                BaseIndices = new List<int>(source.Indices);
                BaseColor4 = new XnaVector4(0.3f, 0.5f, 0.9f, 0.8f);
                VertexColors4 = source.VertexColors4;

                Vertices = new List<XnaVector3>(BaseVertices);
                Indices = new List<int>(BaseIndices);
            }

            public RenderMolecularSurface()
            {
                BaseVertices = new List<XnaVector3>();
                BaseIndices = new List<int>();
                Vertices = new List<XnaVector3>();
                Indices = new List<int>();
                BaseColor4 = XnaVector4.One;
                VertexColors4 = null;
                RedSpots = new List<RedSpot>();
                SurfacePoints = new List<SurfacePoint>();
            }

            public void ApplyShift(XnaVector3 shift)
            {
                CenterOffset = shift;
                Vertices.Clear();
                foreach (var v in BaseVertices)
                    Vertices.Add(v + shift);
                Indices = new List<int>(BaseIndices);
                NormalsCached = false;
                CachedNormals = null;
            }

            public RenderMolecularSurface CloneWithShift(XnaVector3 shift)
            {
                var copy = new RenderMolecularSurface
                {
                    MoleculeId = this.MoleculeId,
                    AtomCount = this.AtomCount,
                    SurfaceArea = this.SurfaceArea,
                    Volume = this.Volume,
                    BaseVertices = new List<XnaVector3>(this.BaseVertices),
                    BaseIndices = new List<int>(this.BaseIndices),
                    BaseColor4 = this.BaseColor4,
                    VertexColors4 = this.VertexColors4 != null ? (XnaVector4[])this.VertexColors4.Clone() : null,
                    RedSpots = new List<RedSpot>(this.RedSpots),
                    SurfacePoints = new List<SurfacePoint>(this.SurfacePoints),
                    CenterOffset = shift
                };

                copy.Vertices = new List<XnaVector3>();
                foreach (var v in copy.BaseVertices)
                    copy.Vertices.Add(v + shift);
                copy.Indices = new List<int>(copy.BaseIndices);

                return copy;
            }
        }

        public class MeshDataXna
        {
            public List<XnaVector3> Vertices { get; set; } = new List<XnaVector3>();
            public List<int> Indices { get; set; } = new List<int>();
            public bool IsValid => Vertices != null && Vertices.Count > 0 && Indices != null && Indices.Count > 0;
            public int TriangleCount => Indices != null ? Indices.Count / 3 : 0;

            public MeshDataXna CloneWithShiftXna(XnaVector3 shift)
            {
                if (this == null || Vertices == null) return new MeshDataXna();
                var shiftedVerts = new List<XnaVector3>(Vertices.Count);
                for (int i = 0; i < Vertices.Count; i++) shiftedVerts.Add(Vertices[i] + shift);
                return new MeshDataXna { Vertices = shiftedVerts, Indices = new List<int>(Indices ?? new List<int>()) };
            }
        }

        public class ChannelPathResultXna
        {
            public bool IsConnected { get; set; }
            public List<XnaVector3> MinPath { get; set; } = new List<XnaVector3>();
            public List<XnaVector3> MaxPath { get; set; } = new List<XnaVector3>();
            public List<XnaVector3> WorldPath { get; set; } = new List<XnaVector3>();
        }

        public struct VertexPositionNormalColor4 : IVertexType
        {
            public XnaVector3 Position;
            public XnaVector3 Normal;
            public XnaVector4 Color;

            public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                new VertexElement(24, VertexElementFormat.Vector4, VertexElementUsage.Color, 0)
            );

            VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

            public VertexPositionNormalColor4(XnaVector3 position, XnaVector3 normal, XnaVector4 color)
            {
                Position = position;
                Normal = normal;
                Color = color;
            }
        }

        #endregion
    }

    #region External Types

    public class RedSpot
    {
        public XnaVector3 Position { get; set; }
        public float Intensity { get; set; }
    }

    public class SurfacePoint
    {
        public XnaVector3 Position { get; set; }
        public float Value { get; set; }
    }

    public class MolecularSurface
    {
        public int MoleculeId { get; set; }
        public int AtomCount { get; set; }
        public float SurfaceArea { get; set; }
        public float Volume { get; set; }
        public List<RedSpot> RedSpots { get; set; } = new List<RedSpot>();
        public List<SurfacePoint> SurfacePoints { get; set; } = new List<SurfacePoint>();
        public List<SysVector3> Vertices { get; set; } = new List<SysVector3>();
        public List<int> Indices { get; set; } = new List<int>();
        public XnaVector4[] VertexColors4 { get; set; } = null;
    }

    #endregion
}