#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Reflection;
using PickObjectsCanceled = Autodesk.Revit.Exceptions.OperationCanceledException;
#endregion

namespace HideCategory
{
  [Transaction(TransactionMode.Manual)]
  public class Command : IExternalCommand
  {
    #region Assembly Attribute Accessors
    /// <summary>
    /// Short cut to get executing assembly
    /// </summary>
    Assembly ExecutingAssembly
    {
      get
      {
        return Assembly.GetExecutingAssembly();
      }
    }

    /// <summary>
    /// Return executing Assembly version string
    /// </summary>
    public string AssemblyVersion
    {
      get
      {
        return ExecutingAssembly.GetName().Version.ToString();
      }
    }
    #endregion // Assembly Attribute Accessors

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
          "Please pick an element to define a category to hide in all views"
          + $" ({AssemblyVersion})");
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
      int nVbadX = 0;
      List<string> viewnameOk = new List<string>();
      List<string> viewnameBad = new List<string>();
      List<string> viewnameBadX = new List<string>();

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
          catch(Exception ex)
          {
            viewnameBadX.Add($"{v.Name}({ex.GetType()})");
            ++nVbadX;

          }
        }
        tx.Commit();
      }
      TaskDialog d = new TaskDialog("Hide Category");
      d.MainInstruction = $"Category '{cat.Name}' hidden"
        + $" in {nVok} views; {nVbad} views skipped:";
      d.MainContent = $"OK: {string.Join(", ", viewnameOk.ToArray())}"
        + $"\r\n\r\nSkipped: {string.Join(", ", viewnameBad.ToArray())}"
        + $"\r\n\r\nSkippedX: {string.Join(", ", viewnameBadX.ToArray())}";
      d.Show();

      return Result.Succeeded;
    }
  }
}
