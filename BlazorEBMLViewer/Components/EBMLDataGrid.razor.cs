﻿using BlazorEBMLViewer.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;
using SpawnDev.BlazorJS;
using SpawnDev.EBML;
using SpawnDev.EBML.Elements;
using System.Linq.Dynamic.Core;

namespace BlazorEBMLViewer.Components
{
    public partial class EBMLDataGrid
    {
        [Inject]
        BlazorJSRuntime JS { get; set; }

        [Inject]
        AppService AppService { get; set; }

        [CascadingParameter]
        public bool DocumentBusy { get; set; }

        [CascadingParameter]
        public Document? Document { get; set; }

        [CascadingParameter]
        public MasterElement? ActiveContainer { get; set; }

        RadzenDataGrid<BaseElement> grid;
        int count;
        IEnumerable<BaseElement> orderDetails = new List<BaseElement>();

        bool IsLoading { get; set; }

        public BaseElement? Selected { get; set; } = null;

        [Parameter]
        public EventCallback<BaseElement> DoubleClick { get; set; }
        [Parameter]
        public EventCallback<BaseElement> Select { get; set; }
        [Parameter]
        public EventCallback<BaseElement> Deselect { get; set; }
        [Parameter]
        public EventCallback<RowContextMenuArgs> RowContextMenu { get; set; }

        async Task RowDoubleClick(DataGridRowMouseEventArgs<BaseElement> args)
        {
            await DoubleClick.InvokeAsync(args.Data);
        }
        MasterElement? _ActiveContainer = null;
        protected override async Task OnParametersSetAsync()
        {
            if (_ActiveContainer != ActiveContainer)
            {
                if (_ActiveContainer != null)
                {
                    _ActiveContainer.OnChanged -= Document_OnChanged;
                }
                await grid.SelectRow(null, true);
                _ActiveContainer = ActiveContainer;
                if (ActiveContainer != null)
                {
                    ActiveContainer.OnChanged += Document_OnChanged;
                }
                if (grid != null)
                {
                    await LoadData(null);
                    await grid.RefreshDataAsync();
                    StateHasChanged();
                }
            }
        }
        bool IsEditing(string columnName, BaseElement order)
        {
            // Comparing strings is quicker than checking the contents of a List, so let the property check fail first.
            if (columnName != nameof(BaseElement.DataString))
            {
                return false;
            }
            return columnEditing == columnName && Editing == order;
        }
        string? columnEditing = null;
        void OnCellClick(DataGridCellMouseEventArgs<BaseElement> args)
        {
            var detail = args.Data;
            // This sets which column is currently being edited.
            if (columnEditing == args.Column.Property && detail == Editing)
            {
                return;
            }
            columnEditing = args.Column.Property;
            // This triggers a save on the previous edit. This can be removed if you are going to batch edits through another method.
            if (Editing != null)
            {
                OnUpdateRow(Editing);
            }
            // only the data column is editable
            if (columnEditing != nameof(BaseElement.DataString))
            {
                return;
            }
            var canEdit = true;
            switch (detail.Type)
            {
                case "date":
                    {

                        break;
                    }
                case "binary":
                    {

                        break;
                    }
                case "master":
                    {
                        canEdit = false;
                        break;
                    }
                case "float":
                case "uinteger":
                case "integer":
                case "utf-8":
                case "string":
                    {

                        break;
                    }
            }
            if (!canEdit) return;
            // This sets the Item to be edited.
            EditRow(detail);
        }
        BaseElement? Editing = null;
        void ResetEdit()
        {
            if (Editing == null) return;
            Editing = null;
            _ = ReloadIt();
        }
        async Task ReloadIt()
        {
            await ReLoadData();
            await grid.RefreshDataAsync();
            StateHasChanged();
        }
        void OnUpdateRow(BaseElement order)
        {
            ResetEdit();

            // dbContext.Update(order);

            // dbContext.SaveChanges();

            // If you were doing row-level edits and handling RowDeselect, you could use the line below to
            // clear edits for the current record.

            //editedFields = editedFields.Where(c => c.Key != order.OrderID).ToList();
        }
        void EditRow(BaseElement order)
        {
            ResetEdit();
            Editing = order;
        }
        async Task RowSelect(BaseElement element)
        {
            if (element == null) return;
            Selected = element;
            await Select.InvokeAsync(element);
        }
        async Task RowDeselect(BaseElement element)
        {
            if (Selected == null) return;
            await Deselect.InvokeAsync(element);
            Selected = null;
        }
        async Task ContextMenu(MouseEventArgs args, BaseElement element)
        {
            await RowContextMenu.InvokeAsync(new RowContextMenuArgs(args, element));
        }
        private async void Document_OnChanged(IEnumerable<BaseElement> elements)
        {
            var element = elements.First();
            //JS.Log("GRID: Document_OnChanged", elements.Count(), element.Depth, element.Name, element.Path);
            if (grid != null)
            {
                await ReLoadData();
                await grid.RefreshDataAsync();
                StateHasChanged();
            }
        }
        LoadDataArgs? lastArgs = new LoadDataArgs();
        string? lastfilter = null;
        Task ReLoadData() => LoadData(null);
        async Task LoadData(LoadDataArgs args)
        {
            args ??= lastArgs ?? new LoadDataArgs();
            lastArgs = args;
            IsLoading = true;
            await Task.Delay(50);
            if (!string.IsNullOrEmpty(args.Filter) && lastfilter != args.Filter)
            {
                args.Skip = 0;
            }
            var query = ActiveContainer?.Data.AsQueryable() ?? new List<BaseElement>().AsQueryable();
            if (!string.IsNullOrEmpty(args.Filter))
            {
                lastfilter = args.Filter;
                query = query.Where(args.Filter);
                count = query.Count();
            }
            else
            {
                count = query.Count();
            }
            if (!string.IsNullOrEmpty(args.OrderBy))
            {
                query = query.OrderBy(args.OrderBy);
            }
            if (args.Skip != null) query = query.Skip(args.Skip.Value);
            if (args.Top != null) query = query.Take(args.Top.Value);
            orderDetails = query.ToList();
            IsLoading = false;
        }
        public static string HumandReadableBytes(double len)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return string.Format("{0:0.##} {1}", len, sizes[order]);
        }
    }
}
