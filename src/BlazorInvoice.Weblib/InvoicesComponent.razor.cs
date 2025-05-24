using BlazorInvoice.Shared;
using BlazorInvoice.Shared.Interfaces;
using BlazorInvoice.Weblib.Modals;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using pax.BBToast;

namespace BlazorInvoice.Weblib
{
    public partial class InvoicesComponent
    {
        InvoiceListRequest request = new();
        int totalCount = 0;
        bool isLoading;
        Virtualize<InvoiceListDto>? virtualizeComponent;
        EditContext editContext = null!;
        ExportModal? exportModal;

        [Inject]
        public IMauiPopupService PopupService { get; set; } = null!;

        [Inject]
        public IToastService ToastService { get; set; } = null!;

        [Parameter]
        public bool Unpaid { get; set; }

        [Parameter]
        public EventCallback OnPaidChanged { get; set; }

        protected override async Task OnInitializedAsync()
        {
            if (Unpaid)
            {
                request.Unpaid = true;
            }
            editContext = new EditContext(request);
            editContext.OnFieldChanged += FieldChanged;
            await SetCount();
            await base.OnInitializedAsync();
        }

        private void FieldChanged(object? sender, FieldChangedEventArgs e)
        {
            _ = Reload();
        }

        public void Reset()
        {
            request.Filter = string.Empty;
            _ = Reload();
        }

        private async Task SetCount(bool dry = false)
        {
            if (!dry)
            {
                isLoading = true;
                await InvokeAsync(() => StateHasChanged());
            }
            totalCount = await InvoiceRepository.GetInvoicesCount(request);
            if (!dry)
            {
                isLoading = false;
                await InvokeAsync(() => StateHasChanged());
            }
        }

        private async ValueTask<ItemsProviderResult<InvoiceListDto>> LoadInvoices(ItemsProviderRequest prRequest)
        {
            request.Skip = prRequest.StartIndex;
            request.Take = Math.Min(prRequest.Count, totalCount - prRequest.StartIndex);

            var invoices = await InvoiceRepository.GetInvoices(request, prRequest.CancellationToken);
            return new ItemsProviderResult<InvoiceListDto>(invoices, totalCount);
        }

        private async Task SortList(MouseEventArgs e, string property)
        {
            var exOrder = request.TableOrders.FirstOrDefault(f => f.PropertyName == property);
            if (e.ShiftKey)
            {
                if (exOrder == null)
                {
                    request.TableOrders.Add(new TableOrder()
                    {
                        PropertyName = property
                    });
                }
                else
                {
                    exOrder.Ascending = !exOrder.Ascending;
                }
            }
            else
            {
                request.TableOrders.Clear();
                request.TableOrders.Add(new TableOrder()
                {
                    PropertyName = property,
                    Ascending = exOrder == null ? false : !exOrder.Ascending
                });
            }
            await Reload();
        }

        private async Task Reload()
        {
            isLoading = true;
            await InvokeAsync(() => StateHasChanged());
            await SetCount(true);
            if (virtualizeComponent != null)
            {
                await InvokeAsync(async () =>
                {
                    await virtualizeComponent.RefreshDataAsync();
                    StateHasChanged();
                });
            }
            isLoading = false;
            await InvokeAsync(() => StateHasChanged());
        }

        private async Task Delete(int invoiceId)
        {
            // TODO: Maui dialog
            var result = await PopupService.DisplayAlert(Loc["Delete Invoice"], Loc["DeleteInvoice"], Loc["Delete"], Loc["Cancel"]);
            if (!result)
            {
                return;
            }
            isLoading = true;
            await InvoiceRepository.DeleteInvoice(invoiceId);
            ToastService.ShowSuccess(Loc["Invoice deleted."]);
            await Reload();
            isLoading = false;
        }

        private async Task Finalize(int invoiceId)
        {
            isLoading = true;
            await InvokeAsync(StateHasChanged);
            var info = await InvoiceRepository.GetInvoice(invoiceId, CancellationToken.None);
            if (info is not null)
            {
                var mapper = new BlazorInvoiceMapper();
                var xmlInvoice = mapper.ToXml(info.InvoiceDto);
                await InvoiceRepository.FinalizeInvoice(invoiceId, xmlInvoice, CancellationToken.None);
            }
            await Reload();
        }

        private async Task SetPaid(int invoiceId, bool isPaid)
        {
            await InvoiceRepository.SetIsPaid(invoiceId, isPaid);
            await Reload();
            await OnPaidChanged.InvokeAsync();
        }

        private async Task Copy(int invoiceId)
        {
            await InvoiceRepository.CreateInvoiceCopy(invoiceId);
            await Reload();
        }

        private void Export(InvoiceListDto invoiceInfo)
        {
            exportModal?.Show(invoiceInfo);
        }
    }
}