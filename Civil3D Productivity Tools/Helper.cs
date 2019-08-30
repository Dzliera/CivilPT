using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.Civil.ApplicationServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace Civil3D_Productivity_Tools
{
    public static class Helper
    {
        #region CodeShortenerProperties
        public static CivilDocument D => CivilApplication.ActiveDocument;
        public static Document AcD => Application.DocumentManager.MdiActiveDocument;

        public static Editor ED => Application.DocumentManager.CurrentDocument.Editor;

        public static Editor AcadED => Application.DocumentManager.MdiActiveDocument.Editor;

        public static Database Db => HostApplicationServices.WorkingDatabase;
        #endregion

        public static NumberFormatInfo NFormat = new NumberFormatInfo() {NumberDecimalDigits = 2};

        /// <summary>
        /// Creates new transaction invokes code on that transaction and than commits transaction
        /// </summary>
        public static void TInvoke(Action<Transaction> action)
        {
            var t = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction();
            action(t);
            t.Commit();
        }
    }
}
