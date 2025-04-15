#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.HMIProject;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Core;
using FTOptix.NetLogic;
using FTOptix.Store;
using FTOptix.SQLiteStore;
using FTOptix.DataLogger;
#endregion

public class DataGridCreator : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void QueryOnStore(string tableName)
    {
        queryTask?.Dispose();
        var arguments = new object[] { tableName };
        queryTask = new LongRunningTask(QueryAndUpdate, arguments, LogicObject);
        queryTask.Start();
    }

    private void QueryAndUpdate(LongRunningTask myTask, object args)
    {
        // Get the table name from the arguments
        var argumentsArray = (object[])args;
        var tableName = (string)argumentsArray[0];

        if (string.IsNullOrEmpty(tableName))
            return;

        // Get the DataGrid and Store
        var store = Project.Current.Get<Store>("DataStores/EmbeddedDatabase1");
        var dataGrid = Owner.Get<DataGrid>("DataGrid");

        if (store == null || dataGrid == null)
            return;

        // Reset the grid
        dataGrid.Query = "";

        // Prepare the query
        var query = $"SELECT * FROM {tableName}";

        // Execute the query
        store.Query(query, out String[] header, out Object[,] resultSet);

        if (header == null || resultSet == null)
            return;

        // Clear existing rows
        dataGrid.Columns.Clear();

        DynamicLink lastDynamicLink = null;

        // Create columns based on the header
        foreach (var columnName in header)
        {
            var newDataGridColumn = InformationModel.MakeObject<DataGridColumn>(columnName);
            newDataGridColumn.Title = columnName;
            newDataGridColumn.DataItemTemplate = InformationModel.MakeObject<DataGridLabelItemTemplate>("DataItemTemplate");
            var dynamicLink = InformationModel.MakeVariable<DynamicLink>("DynamicLink", FTOptix.Core.DataTypes.NodePath);
            dynamicLink.Value = "{Item}/" + NodePath.EscapeNodePathBrowseName(columnName);
            newDataGridColumn.DataItemTemplate.GetVariable("Text").Refs.AddReference(FTOptix.CoreBase.ReferenceTypes.HasDynamicLink, dynamicLink);
            newDataGridColumn.OrderBy = dynamicLink.Value;
            dataGrid.Columns.Add(newDataGridColumn);
            lastDynamicLink = dynamicLink;
        }

        // Set last column as sort item - not really needed
        dataGrid.SortColumnVariable.Refs.AddReference(FTOptix.CoreBase.ReferenceTypes.HasDynamicLink, lastDynamicLink);

        // Add the new query to the grid
        dataGrid.Query = query + " ORDER BY Timestamp DESC";
    }

    private LongRunningTask queryTask;
}
