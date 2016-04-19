using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xaml.Schema;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using SolidworksAddinFramework.OpenGl;
using SolidWorks.Interop.sldworks;

namespace SolidworksAddinFramework
{
    public class TwoPointRange 
    {
        public DenseVector P0 { get; }

        public DenseVector P1 { get; }

        public DenseVector Center => new[]
        {
            (P0[0]+P1[0])/2,
            (P0[1]+P1[1])/2,
            (P0[2]+P1[2])/2
        };

        public TwoPointRange(DenseVector p0, DenseVector p1)
        {
            P0 = p0;
            P1 = p1;
        }

        public IEnumerable<DenseVector> Extremities => 
            from x in XRange
            from y in YRange
            from z in ZRange
            select new DenseVector(new[] { x, y, z });

        public double XMin => Math.Min(P0[0], P1[0]);
        public double XMax => Math.Max(P0[0], P1[0]);
        public double XMid => (XMin + XMax)/2; 

        public double YMin => Math.Min(P0[1], P1[1]);
        public double YMax => Math.Max(P0[1], P1[1]);
        public double YMid => (YMin + YMax)/2; 

        public double ZMin => Math.Min(P0[2], P1[2]);
        public double ZMax => Math.Max(P0[2], P1[2]);
        public double ZMid => (ZMin + ZMax)/2; 

        public double[] XRange => new[] {XMin, XMax};
        public double[] YRange => new[] {YMin, YMax};
        public double[] ZRange => new[] {ZMin, ZMax};

        public double Dx => XMax - XMin;
        public double Dy => YMax - YMin;
        public double Dz => ZMax - ZMin;

        public bool Inside(double[] p)
        {
            var x = p[0];
            var y = p[1];
            var z = p[2];
            return (Between(x, XMin, XMax)&&Between(y,YMin,YMax)&& Between(z, ZMin, ZMax));
        }

        private bool Between(double v, double lower, double upper)
        {
            return v >= lower && v < upper;
        }

        public IBody2 ToSolid()
        {
            var modeler = SwAddinBase.Active.Modeler;
            var math = SwAddinBase.Active.Math;
            var center = new double[] {XMid, YMid, ZMin};
            var box = modeler.CreateBox(center, math.ZAxisArray(), this.Dx, this.Dy, this.Dz);
            Debug.Assert(box!=null);
            return box;
        }

        public Mesh ToMesh()
        {
            return new Mesh(Triangles().ToList());
        }

        public IEnumerable<IReadOnlyList<double[]>> Triangles()
        {
            var _ = new double[2,2,2][];
            for (var i = 0; i < 2; i++)
            {
                for (var j = 0; j < 2; j++)
                {
                    for (var k = 0; k < 2; k++)
                    {
                        _[i,j,k] = new double[]
                        {
                            i == 0?XMin:XMax,
                            j == 0?YMin:YMax,
                            k == 0?ZMin:ZMax,
                        };
                        
                    }
                    
                }
            }

            for (var faceId = 0; faceId < 6; faceId++)
            {
                switch (faceId)
                {
                    case 0: // front
                        yield return new[] {_[0, 0, 0], _[1, 0, 0], _[0, 1, 0] }.Reverse().ToArray();
                        yield return new[] {_[1, 1, 0], _[0, 1, 0], _[1, 0, 0] }.Reverse().ToArray();
                        break;
                    case 1: // back
                        yield return new[] {_[0, 0, 1], _[1, 0, 1], _[0, 1, 1] };
                        yield return new[] {_[1, 1, 1], _[0, 1, 1], _[1, 0, 1] };
                        break;
                    case 2: // left
                        yield return new[] {_[0, 0, 0], _[0, 1, 0], _[0, 0, 1] }.Reverse().ToArray();
                        yield return new[] {_[0, 0, 1], _[0, 1, 0], _[0, 1, 1] }.Reverse().ToArray();
                        break;
                    case 3: // right
                        yield return new[] {_[1, 0, 0], _[1, 1, 0], _[1, 0, 1] };
                        yield return new[] {_[1, 0, 1], _[1, 1, 0], _[1, 1, 1] };
                        break;
                    case 4: // top
                        yield return new[] {_[0, 1, 0], _[1, 1, 1], _[1, 1, 0] };
                        yield return new[] {_[0, 1, 0], _[0, 1, 1], _[1, 1, 1] };
                        break;
                    case 5: // bottom
                        yield return new[] {_[0, 0, 0], _[1, 0, 1], _[1, 0, 0] }.Reverse().ToArray();
                        yield return new[] {_[0, 0, 0], _[0, 0, 1], _[1, 0, 1] }.Reverse().ToArray();
                        break;
                    default:
                        throw new Exception("Out of range value");
                }

                
            }
            
        }
        public static TwoPointRange FromVertices(IReadOnlyList<DenseVector>  vertices)
        {
            var xmin = double.MaxValue;
            var ymin = double.MaxValue;
            var zmin = double.MaxValue;

            var xmax = double.MinValue;
            var ymax = double.MinValue;
            var zmax = double.MinValue;

            // performance optimization. Avoid LINQ
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < vertices.Count; i++)
            {

                var vertex = vertices[i];
                var v0 = vertex[0];
                var v1 = vertex[1];
                var v2 = vertex[2];

                xmin = Math.Min(xmin, v0);
                xmax = Math.Max(xmax, v0);

                ymin = Math.Min(ymin, v1);
                ymax = Math.Max(ymax, v1);

                zmin = Math.Min(zmin, v2);
                zmax = Math.Max(zmax, v2);
            }

            return new TwoPointRange(new DenseVector(new[] {xmin,ymin,zmin}), new DenseVector(new[] {xmax, ymax, zmax}));

        }

        public TwoPointRange Scale(double s)
        {
            s = (s - 1)/2;
            var sx = Dx*s;
            var sy = Dy*s;
            var sz = Dz*s;
            return new TwoPointRange(new[] {XMin-sx,YMin-sy,ZMin-sz}, new[] {XMax+sx, YMax+sy, ZMax+sz});
        }

        public bool Intersects(TwoPointRange other)
        {
            if (XMax < other.XMin)
                return false;
            if (YMax < other.YMin)
                return false;
            if (ZMax < other.ZMin)
                return false;

            if (XMin > other.XMax)
                return false;
            if (YMin > other.YMax)
                return false;
            if (ZMin > other.ZMax)
                return false;

            return true;

        }

    }
}