using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Aec.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using static Civil3D_Productivity_Tools.Helper;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using Exception = System.Exception;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Civil3D_Productivity_Tools
{
    public class MyApp : IExtensionApplication
    {

        #region AppContextEvents
        public void Initialize()
        {

        }

        public void Terminate()
        {

        }
        #endregion

        #region Commands

        [CommandMethod("PtCheckVersion")]
        public void CheckVersion()
        {
            var editor = Application.DocumentManager.CurrentDocument.Editor;
            editor.WriteMessage(AppContext.AppVersion.ToString());
        }

        [CommandMethod("PtBuildCorridorSurfacesAll")]
        public void BuildCorridorSurfacesAll()
        {
            TInvoke(t =>
            {
                foreach (var id in D.CorridorCollection)
                {
                    var corridor = (Corridor)t.GetObject(id, OpenMode.ForWrite);
                    var hasDatum = corridor.CorridorSurfaces.Any(s =>
                    {
                        var links = s.LinkCodes();
                        return links.Length == 1 && links[0].ToLower() == "datum";
                    });
                    var hasTop = corridor.CorridorSurfaces.Any(s =>
                    {
                        var links = s.LinkCodes();
                        return links.Length == 1 && links[0].ToLower() == "top";
                    });

                    if (!hasDatum)
                    {
                        var surface = corridor.CorridorSurfaces.Add($"{corridor.Name} - Datum");
                        surface.Boundaries.AddCorridorExtentsBoundary($"{corridor.Name}");
                    }

                    if (!hasTop)
                    {
                        var surface = corridor.CorridorSurfaces.Add($"{corridor.Name} - Top");
                        surface.Boundaries.AddCorridorExtentsBoundary($"{corridor.Name}");
                    }
                }
            });
        }


        [CommandMethod("GetSelection")]
        public void GetSelection()
        {
            var sOptions = new PromptSelectionOptions()
            {
                PrepareOptionalDetails = true,
                SelectEverythingInAperture = true,
                AllowDuplicates = false,
            };

            var sectionViewName = DBDictionary.GetClass(typeof(Line)).DxfName;
            var filter = new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, sectionViewName), 
            });

            ED.WriteMessage("Select First Section View Group");
            var selection1 = ED.GetSelection(sOptions, filter);

            if (selection1.Status != PromptStatus.OK) return;

            var ids1 = selection1.Value.GetObjectIds();

            var selection2 = ED.GetSelection(sOptions, filter);

            if (selection2.Status != PromptStatus.OK) return;

            var ids2 = selection2.Value.GetObjectIds();

            TInvoke(t =>
            {
                for (var i = 0; i < Math.Min(ids1.Length, ids2.Length); i++)
                {
                    var view1 = (SectionView)t.GetObject(ids1[i], OpenMode.ForRead);
                    var view2 = (SectionView)t.GetObject(ids2[i], OpenMode.ForRead);
                    
                }
            });
        }

        [CommandMethod("A")]
        public void GetCutFillArea()
        {
            var sOptions = new PromptSelectionOptions()
            {
                SingleOnly = true,
                AllowDuplicates = false,
            };

            var polyline = DBDictionary.GetClass(typeof(Polyline)).DxfName;
            var filter = new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, polyline),
            });

            ED.WriteMessage("Select First PLine");
            var selection1 = ED.GetSelection(sOptions, filter);

            if (selection1.Status != PromptStatus.OK) return;

            var id1 = selection1.Value.GetObjectIds()[0];

            ED.WriteMessage("Select Second PLine");
            var selection2 = ED.GetSelection(sOptions, filter);

            if (selection2.Status != PromptStatus.OK) return;

            var id2 = selection2.Value.GetObjectIds()[0];

            TInvoke(t =>
            {
                var p1 = (Polyline)t.GetObject(id1, OpenMode.ForRead);
                var p2 = (Polyline)t.GetObject(id2, OpenMode.ForRead);
                var (cut, fill) = CalculateArea(t, p1, p2);

                ED.WriteMessage($"Cut: {cut.ToString("N", NFormat)}, Fill: {fill.ToString("N", NFormat)}");
                ED.WriteMessage("Select Point To Place Labels:");
                var res = ED.GetPoint(new PromptPointOptions("Pick Point To Place Labels"));
                if (res.Status == PromptStatus.OK)
                {
                    var point = res.Value;
                    var label = new DBText
                    {
                        TextString = $"{cut.ToString("N", NFormat)}",
                        Height = 0.4,
                        Position = point,
                        Color = Color.FromColor(System.Drawing.Color.Red)
                    };

                    var label2 = new DBText
                    {
                        TextString = $"{fill.ToString("N", NFormat)}",
                        Height = 0.4,
                        Position = point.Add(new Vector3d(3, 0, 0)),
                        Color = Color.FromColor(System.Drawing.Color.Green)
                    };

                    var btr = (BlockTableRecord)t.GetObject(Db.CurrentSpaceId, OpenMode.ForWrite);
                    btr.AppendEntity(label);
                    btr.AppendEntity(label2);
                    t.AddNewlyCreatedDBObject(label, true);
                    t.AddNewlyCreatedDBObject(label2, true);
                }
            });
        }

        private Tuple<double, double> CalculateArea(Transaction t, Polyline p1, Polyline p2)
        {
            Point3d startPoint1;
            Point3d startPoint2;
            if (p1.StartPoint.X > p2.StartPoint.X)
            {
                startPoint2 = p2.GetClosestPointTo(p1.StartPoint, Vector3d.YAxis, true);
                startPoint1 = p1.StartPoint;
            }
            else
            {
                startPoint1 = p1.GetClosestPointTo(p2.StartPoint, Vector3d.YAxis, true);
                startPoint2 = p2.StartPoint;
            }


            Point3d endPoint1;
            Point3d endPoint2;
            if (p1.EndPoint.X < p2.EndPoint.X)
            {
                endPoint2 = p2.GetClosestPointTo(p1.EndPoint, Vector3d.YAxis, true);
                endPoint1 = p1.EndPoint;
            }
            else
            {
                endPoint1 = p1.GetClosestPointTo(p2.EndPoint, Vector3d.YAxis, true);
                endPoint2 = p2.EndPoint;
            }

            var btr = (BlockTableRecord)t.GetObject(Db.CurrentSpaceId, OpenMode.ForWrite);

            var pts = new Point3dCollection();

            p1.IntersectWith(p2, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);


            var interArray = new Point3d[pts.Count];
            pts.CopyTo(interArray, 0);
            var interList = interArray.ToList();
            interList.Sort((pt1, pt2) => (int)Math.Floor(pt1.X - pt2.X));
            var interQueue = new Queue<Point3d>(interList);

            var cutArea = 0.0;
            var fillArea = 0.0;

            var curPolyline = new Polyline();
            var VertexYSum = 0.0;
            var curInd = 1;
            var curInterPt = interQueue.Count > 0? interQueue.Dequeue() : endPoint1;
            var i2 = 0;

            curPolyline.AddVertexAt(0, new Point2d(startPoint1.X, startPoint1.Y), 0, 0, 0);
            VertexYSum += startPoint1.Y;
            curPolyline.AddVertexAt(1, new Point2d(startPoint2.X, startPoint2.Y), 0, 0, 0);

            for (var i = 0; i < p1.NumberOfVertices; i++)
            {
                // Could also get the 3D point here
                var pt = p1.GetPoint2dAt(i);
                if(pt.X <= startPoint1.X)continue;
                if (pt.X > curInterPt.X)
                {
                    var vertexAvY = VertexYSum / curInd;
                    curPolyline.AddVertexAt(curInd++, new Point2d(curInterPt.X, curInterPt.Y), 0, 0, 0);
                    if(curInterPt == endPoint1)
                        curPolyline.AddVertexAt(curInd++, new Point2d(endPoint2.X, endPoint2.Y), 0, 0, 0);
                    var reverseList = new List<Point2d>();
                    for (; i2 < p2.NumberOfVertices; i2 ++)
                    {
                        var pt2 = p2.GetPoint2dAt(i2);
                        if (pt.X <= startPoint1.X) continue;
                        if(pt2.X <= curInterPt.X) reverseList.Add(pt2);
                        if (pt2.X >= curInterPt.X)
                        {
                            reverseList.Reverse();
                            var vertexYSum2 = 0.0;
                            reverseList.ForEach(p =>
                            {
                                curPolyline.AddVertexAt(curInd++, p, 0, 0, 0);
                                vertexYSum2 += pt.Y;
                            });
                            var vertexAvY2 = vertexYSum2 / reverseList.Count;
                            curPolyline.Closed = true;
                            var isCut = vertexAvY < vertexAvY2;
                            if (isCut) cutArea += curPolyline.Area;
                            else fillArea += curPolyline.Area;
                            var lPoint = CalculateMedianPoint(curPolyline);
                            var label = new DBText
                            {
                                TextString = curPolyline.Area.ToString("N",new NumberFormatInfo(){NumberDecimalDigits = 2}),
                                Height = 0.2,
                                Position = new Point3d(lPoint.X, lPoint.Y, 0 )
                            };

                            btr.AppendEntity(label);
                            t.AddNewlyCreatedDBObject(label, true);

                            if(curInterPt == endPoint1) return new Tuple<double, double>(cutArea, fillArea);
                            curPolyline = new Polyline();
                            VertexYSum = 0;
                            curInd = 1;
                            curPolyline.AddVertexAt(0, new Point2d(curInterPt.X, curInterPt.Y),  0,0, 0);
                            VertexYSum += curInterPt.Y;
                            curInterPt = interQueue.Count > 0 ? interQueue.Dequeue() : endPoint1;
                            break;
                        }
                    }
                }

                curPolyline.AddVertexAt(curInd++, pt, 0, 0, 0);
                VertexYSum += pt.Y;

            }

            return new Tuple<double, double>(cutArea, fillArea);
        }


        private Tuple<double, double> CalculateArea(Transaction t, Polyline pEx, Polyline pMid, Polyline pDesign)
        {
            var start = (new List<double>(){pEx.StartPoint.X, pMid.StartPoint.X, pDesign.StartPoint.X}).Max();
            var end = (new List<double>(){pEx.EndPoint.X, pMid.EndPoint.X, pDesign.EndPoint.X}).Min();

            var minY = double.MaxValue;
            var maxY = double.MinValue;

            UpdateMaxMinY(pEx, ref minY, ref maxY);
            UpdateMaxMinY(pMid, ref minY, ref maxY);
            UpdateMaxMinY(pDesign, ref minY, ref maxY);

            var step = 0.1;
            var cut = 0d;
            var fill = 0d;

            for (var i = start; i < end; i+=step)
            {
                var line = new  Line(new Point3d(i, minY, 0), new Point3d(i, maxY, 0));
                var col = new Point3dCollection();
                line.IntersectWith(pEx, Intersect.OnBothOperands, col, IntPtr.Zero,  IntPtr.Zero);
                var pExY = col[0].Y;
                col = new Point3dCollection();
                line.IntersectWith(pMid, Intersect.OnBothOperands, col, IntPtr.Zero, IntPtr.Zero);
                var pMidY = col[0].Y;
                col = new Point3dCollection();
                line.IntersectWith(pMid, Intersect.OnBothOperands, col, IntPtr.Zero, IntPtr.Zero);
                var pDesignY = col[0].Y;
                if (pDesignY > pExY)
                {
                    if(pMidY < pExY) continue;
                    if (pMidY > pExY && pMidY < pDesignY) fill += pMidY - pExY;
                }
            }
            


            throw new NotImplementedException();
        }

        private static void UpdateMaxMinY(Polyline pol,  ref double minY,  ref double maxY)
        {
            for (int i = 0; i < pol.NumberOfVertices; i++)
            {
                var p = pol.GetPoint2dAt(i);
                var y = p.Y;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }


        private Point2d CalculateMedianPoint(Polyline pol)
        {
            double xSum = 0;
            double ySum = 0;

            for (int i = 0; i < pol.NumberOfVertices; i++)
            {
                var p = pol.GetPoint2dAt(i);
                xSum += p.X;
                ySum += p.Y;
            }

            return new Point2d(xSum / pol.NumberOfVertices, ySum / pol.NumberOfVertices);
        }

        #endregion
    }
}
