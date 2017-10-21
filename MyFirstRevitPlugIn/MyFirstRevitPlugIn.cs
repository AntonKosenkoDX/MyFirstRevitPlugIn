using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;

namespace MyFirstRevitPlugIn
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class MyFirstRevitPlugIn : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData, 
            ref string message, 
            ElementSet elements
            )
        {
            UIApplication uiApp = commandData.Application;
            Document doc = uiApp.ActiveUIDocument.Document;

            Reference pickedRef = null;

            Selection sel = uiApp.ActiveUIDocument.Selection;
            pickedRef = sel.PickObject(
                ObjectType.Element,
                "Please, select group"
                );
            Element elem = doc.GetElement(pickedRef);
            Group group = elem as Group;

            XYZ point = sel.PickPoint("Please pick a point to place group");

            Transaction trans = new Transaction(doc);
            trans.Start("MyFirstPlugIn");
            doc.Create.PlaceGroup(point, group.GroupType);
            trans.Commit();

            return Result.Succeeded;
        }
    }
}
