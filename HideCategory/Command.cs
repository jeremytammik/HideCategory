#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Collections.Generic;
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
      int nVok = 0;
      int nVbad = 0;
      List<string> viewnameOk = new List<string>();
      List<string> viewnameBad = new List<string>();

      using (Transaction tx = new Transaction(doc))
      {
        tx.Start($"Hide category '{cat.Name}' in all views");
        foreach (View v in views)
        {
          try
          {
            v.SetCategoryHidden(cid, hide);
            viewnameOk.Add(v.Name);
            ++nVok;
          }
          catch(Autodesk.Revit.Exceptions.ArgumentException)
          {
            viewnameBad.Add(v.Name);
            ++nVbad;
          }
        }
        tx.Commit();
      }
      TaskDialog d = new TaskDialog("Hide Category");
      d.MainInstruction = $"Category '{cat.Name}' hidden"
        + " in {nVok} views; {nVbad} views skipped:";
      d.MainContent = $"OK: {string.Join(", ", viewnameOk.ToArray())}"
        + "\r\nSkipped: {string.Join(\", \", viewnameBad.ToArray())}";
      d.Show();

      return Result.Succeeded;
    }
  }
}
