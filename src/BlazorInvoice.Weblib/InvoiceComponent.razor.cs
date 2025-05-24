using BlazorInvoice.Pdf;
using BlazorInvoice.Shared;
using BlazorInvoice.Shared.Interfaces;
using BlazorInvoice.Weblib.Modals;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using pax.BBToast;
using pax.XRechnung.NET;
using pax.XRechnung.NET.AnnotatedDtos;
using System.Data;

namespace BlazorInvoice.Weblib;

public partial class InvoiceComponent
{
    [Inject]
    public IInvoiceRepository InvoiceRepository { get; set; } = null!;

    [Inject]
    public IToastService ToastService { get; set; } = null!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    public IMauiPopupService PopupService { get; set; } = null!;

    [Parameter]
    public int? InvoiceId { get; set; }

    [Parameter]
    public bool DoContinue { get; set; }

    private List<PartyListDto>? Sellers;
    private List<PartyListDto>? Buyers;
    private List<PaymentListDto>? Payments;

    private int? SelectedSellerId;
    private int? SelectedBuyerId;
    private int? SelectedPaymentId;
    private int SelectedLineIndex;
    private bool isLineEdit = false;

    private bool ShowSellerCreation = false;
    private bool ShowBuyerCreation = false;
    private bool ShowPaymentCreation = false;
    private bool ShowLineCreation = false;
    private bool ShowXmlText = false;
    private bool ShowValidation = false;
    private bool ShowChatGpt = false;
    private bool ShowPdfComponent { get; set; } = true;

    private BlazorInvoiceDto invoiceDto = new()
    {
        IssueDate = DateTime.Today,
        DueDate = DateTime.Today.AddDays(14)
    };
    private EditContext sellerEditContext = null!;
    private EditContext buyerEditContext = null!;
    private EditContext paymentEditContext = null!;
    private EditContext invoiceEditContext = null!;

    TempInvoiceModal? tempInvoiceModal;
    bool hasTempInvoice;
    SemaphoreSlim tempSs = new(1, 1);

    InvoicePdfComponent? invoicePdfComponent;
    LineImportModal? lineImportModal;
    XmlTextComponent? xmlTextComponent;
    CodeListModal? codeListModal;
    ExportModal? exportModal;
    ChatGptComponent? chatGptComponent;

    InvoiceValidationResult? invoiceValidationResult = null;

    private bool AllRequiredSelected =>
        SelectedSellerId.HasValue && SelectedBuyerId.HasValue && SelectedPaymentId.HasValue;

    private bool NoneRequiredSelected =>
        !InvoiceId.HasValue && !SelectedSellerId.HasValue && !SelectedBuyerId.HasValue && !SelectedPaymentId.HasValue;

    AppConfigDto appConfig = null!;
    private bool showDesc => appConfig.ShowFormDescriptions;

    protected override async Task OnInitializedAsync()
    {
        Sellers = await InvoiceRepository.GetSellers(new InvoiceListRequest());
        Buyers = await InvoiceRepository.GetBuyers(new InvoiceListRequest());
        Payments = await InvoiceRepository.GetPayments(new InvoiceListRequest());
        appConfig = await ConfigService.GetConfig();

        if (InvoiceId is not null)
        {
            var info = await InvoiceRepository.GetInvoice(InvoiceId.Value);
            if (info is not null)
            {
                invoiceDto = info.InvoiceDto;
                SelectedSellerId = info.SellerId;
                SelectedBuyerId = info.BuyerId;
                SelectedPaymentId = info.PaymentId;
            }
        }
        CreateEditContexts();
        NavigationManager.LocationChanged += LeavePage;
        hasTempInvoice = await InvoiceRepository.HasTempInvoice();
    }

    private async void LeavePage(object? sender, LocationChangedEventArgs e)
    {
        if (AllRequiredSelected && hasTempInvoice)
        {
            var result = await PopupService.DisplayAlert(Loc["Warning"], Loc["SaveWarning"], Loc["Save"], Loc["No"]);
            if (result)
            {
                await OnInvoiceChanged(invoiceDto);
            }
        }
    }

    public override void UpdateState(object? sender)
    {
        invoicePdfComponent?.Update(invoiceDto);
        base.UpdateState(sender);
    }

    private void CreateEditContexts()
    {
        sellerEditContext = new EditContext(invoiceDto.SellerParty);
        buyerEditContext = new EditContext(invoiceDto.BuyerParty);
        paymentEditContext = new EditContext(invoiceDto.PaymentMeans);
        invoiceEditContext = new EditContext(invoiceDto);

        sellerEditContext.OnFieldChanged += FieldChanged;
        buyerEditContext.OnFieldChanged += FieldChanged;
        paymentEditContext.OnFieldChanged += FieldChanged;
        invoiceEditContext.OnFieldChanged += FieldChanged;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && hasTempInvoice)
        {
            if (DoContinue)
            {
                _ = ContinueTempInvoice(true);
            }
            else
            {
                tempInvoiceModal?.Show();
            }
            if (AllRequiredSelected)
            {
                UpdateVisuals();
            }
        }
        base.OnAfterRender(firstRender);
    }

    public void UpdateVisuals()
    {
        invoicePdfComponent?.Update(invoiceDto);
        xmlTextComponent?.Update(invoiceDto);
        chatGptComponent?.Update(invoiceDto);
    }

    private async Task OnSellerSelected()
    {
        if (SelectedSellerId is null)
        {
            return;
        }
        invoiceDto.SellerParty = await InvoiceRepository.GetSeller(SelectedSellerId.Value) ?? new();
        sellerEditContext = new EditContext(invoiceDto.SellerParty);
        sellerEditContext.OnFieldChanged += FieldChanged;
        ShowSellerCreation = false;
        await InvokeAsync(StateHasChanged);
        _ = SaveTempInvoice();
    }

    private async Task OnBuyerSelected()
    {
        if (SelectedBuyerId is null)
        {
            return;
        }
        invoiceDto.BuyerParty = await InvoiceRepository.GetBuyer(SelectedBuyerId.Value) ?? new();
        buyerEditContext = new EditContext(invoiceDto.BuyerParty);
        buyerEditContext.OnFieldChanged += FieldChanged;
        ShowBuyerCreation = false;
        await InvokeAsync(StateHasChanged);
        _ = SaveTempInvoice();
    }

    private async Task OnPaymentSelected()
    {
        if (SelectedPaymentId is null)
        {
            return;
        }
        invoiceDto.PaymentMeans = await InvoiceRepository.GetPaymentMeans(SelectedPaymentId.Value) ?? new();
        paymentEditContext = new EditContext(invoiceDto.PaymentMeans);
        paymentEditContext.OnFieldChanged += FieldChanged;
        ShowPaymentCreation = false;
        await InvokeAsync(StateHasChanged);
        _ = SaveTempInvoice();
    }

    private async Task OnLineChanged(InvoiceLineAnnotationDto args)
    {
        ShowLineCreation = false;
        await InvokeAsync(StateHasChanged);
        await SaveTempInvoice();
    }

    private async Task OnSellerChanged(SellerAnnotationDto args)
    {
        if (SelectedSellerId == null)
        {
            SelectedSellerId = await InvoiceRepository.CreateParty(args, true);
            invoiceDto.SellerParty = args;
            sellerEditContext = new EditContext(invoiceDto.SellerParty);
            sellerEditContext.OnFieldChanged += FieldChanged;
        }
        else
        {
            await InvoiceRepository.UpdateParty(SelectedSellerId.Value, invoiceDto.SellerParty);
        }
        Sellers = await InvoiceRepository.GetSellers(new InvoiceListRequest());
        ShowSellerCreation = false;
        await InvokeAsync(StateHasChanged);
        _ = SaveTempInvoice();
    }

    private async Task OnBuyerChanged(BuyerAnnotationDto args)
    {
        if (SelectedBuyerId == null)
        {
            SelectedBuyerId = await InvoiceRepository.CreateParty(args, false);
            invoiceDto.BuyerParty = args;
            buyerEditContext = new EditContext(invoiceDto.BuyerParty);
            buyerEditContext.OnFieldChanged += FieldChanged;
        }
        else
        {
            await InvoiceRepository.UpdateParty(SelectedBuyerId.Value, invoiceDto.BuyerParty);
        }
        Buyers = await InvoiceRepository.GetBuyers(new InvoiceListRequest());
        ShowBuyerCreation = false;
        await InvokeAsync(StateHasChanged);
        _ = SaveTempInvoice();
    }

    private async Task OnPaymentChanged(PaymentAnnotationDto args)
    {
        if (SelectedPaymentId is null)
        {
            SelectedPaymentId = await InvoiceRepository.CreatePaymentMeans(args);
            invoiceDto.PaymentMeans = args;
            paymentEditContext = new EditContext(invoiceDto.PaymentMeans);
            paymentEditContext.OnFieldChanged += FieldChanged;
        }
        else
        {
            await InvoiceRepository.UpdatePaymentMeans(SelectedPaymentId.Value, invoiceDto.PaymentMeans);
        }
        Payments = await InvoiceRepository.GetPayments(new InvoiceListRequest());
        ShowPaymentCreation = false;
        await InvokeAsync(StateHasChanged);
        _ = SaveTempInvoice();
    }

    private void RemoveSeller()
    {
        SelectedSellerId = null;
        invoiceDto.SellerParty = new SellerAnnotationDto();
        sellerEditContext = new EditContext(invoiceDto.SellerParty);
        sellerEditContext.OnFieldChanged += FieldChanged;
        ShowSellerCreation = false;
        _ = SaveTempInvoice();
    }

    private void RemoveBuyer()
    {
        SelectedBuyerId = null;
        invoiceDto.BuyerParty = new BuyerAnnotationDto();
        buyerEditContext = new EditContext(invoiceDto.BuyerParty);
        buyerEditContext.OnFieldChanged += FieldChanged;
        ShowBuyerCreation = false;
        _ = SaveTempInvoice();
    }

    private void RemovePayment()
    {
        SelectedPaymentId = null;
        invoiceDto.PaymentMeans = new PaymentAnnotationDto();
        paymentEditContext = new EditContext(invoiceDto.PaymentMeans);
        paymentEditContext.OnFieldChanged += FieldChanged;
        ShowPaymentCreation = false;
        _ = SaveTempInvoice();
    }

    private async Task OnInvoiceChanged(BlazorInvoiceDto args)
    {
        if (SelectedSellerId == null || SelectedBuyerId == null || SelectedPaymentId == null)
        {
            return;
        }
        var success = invoiceEditContext.Validate();
        if (!success)
        {
            ToastService.ShowError("Invoice incomplete, please check validation messages.");
            return;
        }
        if (InvoiceId is null)
        {
            InvoiceId = await InvoiceRepository.CreateInvoice(args, SelectedSellerId.Value, SelectedBuyerId.Value,
            SelectedPaymentId.Value);
            ToastService.ShowSuccess("Invoice successfully saved.");
        }
        else
        {
            await InvoiceRepository.UpdateInvoice(InvoiceId.Value, args);
            ToastService.ShowSuccess("Invoice successfully updated.");
        }
        await InvoiceRepository.DeleteTempInvoice();
        await InvokeAsync(StateHasChanged);
    }

    private void AddLine()
    {
        invoiceDto.InvoiceLines.Add(new InvoiceLineAnnotationDto());
        SelectedLineIndex = invoiceDto.InvoiceLines.Count - 1;
        isLineEdit = false;
        ShowLineCreation = true;
        SetLineIds();
    }

    private void EditLine(int index)
    {
        SelectedLineIndex = index;
        isLineEdit = true;
        ShowLineCreation = true;
    }

    private void RemoveLine(int index)
    {
        invoiceDto.InvoiceLines.RemoveAt(index);
        invoiceEditContext = new EditContext(invoiceDto);
        invoiceEditContext.OnFieldChanged += FieldChanged;
        ShowLineCreation = false;
        SetLineIds();
    }

    private void MoveLineUp(int index)
    {
        if (index > 0)
        {
            var line = invoiceDto.InvoiceLines[index];
            invoiceDto.InvoiceLines.RemoveAt(index);
            invoiceDto.InvoiceLines.Insert(index - 1, line);
            SetLineIds();
        }
    }

    private void MoveLineDown(int index)
    {
        if (index < invoiceDto.InvoiceLines.Count - 1)
        {
            var line = invoiceDto.InvoiceLines[index];
            invoiceDto.InvoiceLines.RemoveAt(index);
            invoiceDto.InvoiceLines.Insert(index + 1, line);
            SetLineIds();
        }
    }

    private void SetLineIds()
    {
        for (int i = 0; i < invoiceDto.InvoiceLines.Count; i++)
        {
            invoiceDto.InvoiceLines[i].Id = (i + 1).ToString();
        }
        FieldChanged(null, new(new()));
    }

    private void FieldChanged(object? sender, FieldChangedEventArgs e)
    {
        _ = SaveTempInvoice();
        if (AllRequiredSelected)
        {
            UpdateVisuals();
        }
    }

    private async Task SaveTempInvoice()
    {
        await tempSs.WaitAsync();
        try
        {
            await InvoiceRepository.SaveTempInvoice(new()
            {
                InvoiceDto = invoiceDto,
                InvoiceId = InvoiceId ?? 0,
                SellerId = SelectedSellerId,
                BuyerId = SelectedBuyerId,
                PaymentId = SelectedPaymentId
            });
            hasTempInvoice = true;
        }
        finally
        {
            tempSs.Release();
        }
    }

    private async Task ContinueTempInvoice(bool continueInvoice)
    {
        // TODO notifications
        if (!continueInvoice)
        {
            await InvoiceRepository.DeleteTempInvoice();
        }
        else
        {
            var response = await InvoiceRepository.GetTempInvoice();
            if (response == null)
            {
                return;
            }
            invoiceDto = response.InvoiceDto;
            CreateEditContexts();
            InvoiceId = response.InvoiceId > 0 ? response.InvoiceId : null;
            SelectedSellerId = response.SellerId;
            SelectedBuyerId = response.BuyerId;
            SelectedPaymentId = response.PaymentId;
            await InvokeAsync(StateHasChanged);
        }
    }

    private string GetXmlText()
    {
        var mapper = new BlazorInvoiceMapper();
        var xmlInvoice = mapper.ToXml(invoiceDto);
        var xmlText = XmlInvoiceWriter.Serialize(xmlInvoice);
        return xmlText;
    }

    private void ValidateInvoiceSchema()
    {
        if (invoiceEditContext is not null)
        {
            invoiceEditContext.Validate();
        }
        var mapper = new BlazorInvoiceMapper();
        var xmlInvoice = mapper.ToXml(invoiceDto);
        invoiceValidationResult = XmlInvoiceValidator.Validate(xmlInvoice);
    }

    private async Task ValidateInvoiceSchematron()
    {
        invoiceEditContext?.Validate();
        if (string.IsNullOrEmpty(appConfig.SchematronValidationUri))
        {
            ToastService.ShowError($"Kosit URI not set. Please check your settings.");
            return;
        }
        var mapper = new BlazorInvoiceMapper();
        var xmlInvoice = mapper.ToXml(invoiceDto);
        var uri = new Uri(appConfig.SchematronValidationUri);
        invoiceValidationResult = await XmlInvoiceValidator.ValidateSchematron(xmlInvoice, uri);
        await InvokeAsync(StateHasChanged);
    }

    private async Task EmbedSellerLogo()
    {
        if (SelectedSellerId is null)
        {
            return;
        }
        var sellerLogo = await InvoiceRepository.GetPartyLogo(SelectedSellerId.Value);
        invoiceDto.EmbedSellerLogo(sellerLogo);
        invoicePdfComponent?.Update(invoiceDto);
        if (InvoiceId is not null)
        {
            await InvoiceRepository.AddReplaceOrDeleteSellerLogo(InvoiceId.Value, default);
        }
    }

    private void CodeListRequest(string codeList)
    {
        codeListModal?.Show(codeList);
    }

    private void CodeListSelected(KeyValuePair<string, string> ent)
    {
        var codeList = ent.Key;
        var value = ent.Value;
        if (codeList == "UNTDID_5305")
        {
            invoiceDto.GlobalTaxCategory = value;
            ToastService.ShowInfo("Global Tax Category set to " + value);
        }
        else if (codeList == "UNTDID_1001")
        {
            invoiceDto.InvoiceTypeCode = value;
            ToastService.ShowInfo("Invoice Type Code set to " + value);
        }
        else if (codeList == "UNTDID_4461")
        {
            invoiceDto.PaymentMeans.PaymentMeansTypeCode = value;
            ToastService.ShowInfo("Payment Means Type Code set to " + value);
        }
        else if (codeList == "Currency_Codes")
        {
            invoiceDto.DocumentCurrencyCode = value;
            ToastService.ShowInfo("Document Currency Code set to " + value);
        }
        else if (codeList == "Country_Codes")
        {
            if (ShowSellerCreation)
            {
                invoiceDto.SellerParty.CountryCode = value;
                ToastService.ShowInfo("Seller Country Code set to " + value);
            }
            else if (ShowBuyerCreation)
            {
                invoiceDto.BuyerParty.CountryCode = value;
                ToastService.ShowInfo("Buyer Country Code set to " + value);
            }

        }
        else if (codeList == "UN_ECE_Recommendation_N_20")
        {
            var line = invoiceDto.InvoiceLines.ElementAtOrDefault(SelectedLineIndex);
            if (line != null)
            {
                line.QuantityCode = value;
                ToastService.ShowInfo("Quantity Code set to " + value);
            }
        }
        invoiceEditContext.Validate();
        FieldChanged(null, new FieldChangedEventArgs(new FieldIdentifier()));
    }

    private async Task CreateSampleInvoice()
    {
        InvoiceId = await InvoiceRepository.SeedTestInvoice();
        if (InvoiceId is not null)
        {
            var info = await InvoiceRepository.GetInvoice(InvoiceId.Value);
            if (info is not null)
            {
                invoiceDto = info.InvoiceDto;
                SelectedSellerId = info.SellerId;
                SelectedBuyerId = info.BuyerId;
                SelectedPaymentId = info.PaymentId;
            }
        }
        CreateEditContexts();
        await InvokeAsync(StateHasChanged);
    }

    public override void Dispose()
    {
        sellerEditContext.OnFieldChanged -= FieldChanged;
        buyerEditContext.OnFieldChanged -= FieldChanged;
        paymentEditContext.OnFieldChanged -= FieldChanged;
        invoiceEditContext.OnFieldChanged -= FieldChanged;
        NavigationManager.LocationChanged -= LeavePage;
        tempSs.Dispose();
        base.Dispose();
    }
}
