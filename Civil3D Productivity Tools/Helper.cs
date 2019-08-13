using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;

namespace Civil3D_Productivity_Tools
{
    public static class Helper
    {
        #region CodeShortenerProperties
        public static CivilDocument D = CivilApplication.ActiveDocument;
        #endregion


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
