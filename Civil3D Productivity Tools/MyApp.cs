using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using static Civil3D_Productivity_Tools.Helper;
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

        #endregion
    }
}
