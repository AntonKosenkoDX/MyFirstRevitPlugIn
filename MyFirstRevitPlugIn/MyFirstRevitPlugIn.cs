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

            try
            {
                Reference pickedRef = null;

                Selection sel = uiApp.ActiveUIDocument.Selection;
                GroupPickFilter selFilter = new GroupPickFilter();
                pickedRef = sel.PickObject(ObjectType.Element, selFilter,
                  "Please select a group");
                Element elem = doc.GetElement(pickedRef);
                Group group = elem as Group;

                XYZ origin = GetElementCenter(group);
                Room room = GetRoomOfGroup(doc, origin);

                XYZ sourceCenter = GetRoomCenter(room);

                RoomPickFilter roomPickFilter = new RoomPickFilter();
                IList<Reference> rooms =
                  sel.PickObjects(
                    ObjectType.Element,
                    roomPickFilter,
                    "Select target rooms for duplicate furniture group");

                Transaction trans = new Transaction(doc);
                trans.Start("Lab");
                PlaceFurnitureInRooms(
                  doc, rooms, sourceCenter,
                  group.GroupType, origin);
                trans.Commit();

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private XYZ GetElementCenter(Element elem)
        {
            BoundingBoxXYZ bounding = elem.get_BoundingBox(null);
            XYZ center = (bounding.Max + bounding.Min) * 0.5;
            return center;
        }

        private Room GetRoomOfGroup(Document doc, XYZ point)
        {
            FilteredElementCollector collector =
              new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms);
            Room room = null;
            foreach (Element elem in collector)
            {
                room = elem as Room;
                if (room != null)
                {
                    if (room.IsPointInRoom(point))
                    {
                        break;
                    }
                }
            }
            return room;
        }

        private XYZ GetRoomCenter(Room room)
        {
            XYZ boundCenter = GetElementCenter(room);
            LocationPoint locPt = (LocationPoint)room.Location;
            XYZ roomCenter =
              new XYZ(boundCenter.X, boundCenter.Y, locPt.Point.Z);
            return roomCenter;
        }

        public void PlaceFurnitureInRooms(
                        Document doc,
                        IList<Reference> rooms,
                        XYZ sourceCenter,
                        GroupType gt,
                        XYZ groupOrigin)
        {
            XYZ offset = groupOrigin - sourceCenter;
            XYZ offsetXY = new XYZ(offset.X, offset.Y, 0);

            foreach (Reference r in rooms)
            {
                Room roomTarget = doc.GetElement(r) as Room;
                if (roomTarget != null)
                {
                    XYZ roomCenter = GetRoomCenter(roomTarget);
                    Group group =
                      doc.Create.PlaceGroup(roomCenter + offsetXY, gt);
                }
            }
        }

    }
    internal class GroupPickFilter : ISelectionFilter
    {
        public bool AllowElement(Element e)
        {
            return (e.Category.Id.IntegerValue.Equals(
              (int)BuiltInCategory.OST_IOSModelGroups));
        }
        public bool AllowReference(Reference r, XYZ p)
        {
            return false;
        }
    }

    internal class RoomPickFilter : ISelectionFilter
    {
        public bool AllowElement(Element e)
        {
            return (e.Category.Id.IntegerValue.Equals(
              (int)BuiltInCategory.OST_Rooms));
        }

        public bool AllowReference(Reference r, XYZ p)
        {
            return false;
        }
    }

}
