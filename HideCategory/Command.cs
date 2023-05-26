#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using PickObjectsCanceled = Autodesk.Revit.Exceptions.OperationCanceledException;
#endregion

namespace HideCategory
{
  [Transaction(TransactionMode.Manual)]
  public class Command : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements)
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;
      Element e = null;

      try
      {
        Selection sel = uidoc.Selection;
        Reference r = sel.PickObject(ObjectType.Element, 
          "Please pick an element to define a category to hide in all views");
        e = doc.GetElement(r.ElementId);
      }
      catch (PickObjectsCanceled)
      {
        return Result.Cancelled;
      }
      FilteredElementCollector views
        = new FilteredElementCollector(doc)
          .OfClass(typeof(View));

      Category cat = e.Category;
      ElementId cid = cat.Id;
      bool hide = true;

      using (Transaction tx = new Transaction(doc))
      {
        tx.Start($"Hide category '{cat.Name}' in all views");
        foreach (View v in views)
        {
          try
          {
            v.SetCategoryHidden(cid, hide);
          }
          catch(Autodesk.Revit.Exceptions.ArgumentException)
          {
          }
        }
        tx.Commit();
      }
      return Result.Succeeded;
    }
  }
}
