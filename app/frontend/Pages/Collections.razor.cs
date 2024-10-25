// Copyright (c) Microsoft. All rights reserved.

using MinimalApi.Services.Documents;
using Shared.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ClientApp.Pages;

public sealed partial class Collections : IDisposable
{
    private const long MaxIndividualFileSize = 1_024 * 1_024 * 10;

    private MudForm _form = null!;

    private IList<IBrowserFile> _files = new List<IBrowserFile>();
    //private MudFileUpload<IReadOnlyList<IBrowserFile>> _fileUpload = null!;
    private Task _getDocumentsTask = null!;
    private bool _isLoadingDocuments = false;
    private bool _isUploadingDocuments = false;
    private bool _isIndexingDocuments = false;
    private string _filter = "";

    // Store a cancelation token that will be used to cancel if the user disposes of this component.
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly HashSet<DocumentSummary> _documents = [];

    [Inject] public required ApiClient Client { get; set; }
    [Inject] public required ISnackbar Snackbar { get; set; }
    [Inject] public required ILogger<Docs> Logger { get; set; }
    [Inject] public required IJSRuntime JSRuntime { get; set; }
    [Inject] public required HttpClient HttpClient { get; set; }

    //private bool FilesSelected => _fileUpload is { _files.: > 0 };

    private List<ProfileSummary> _profiles = new();
    private ProfileSummary? _selectedProfileSummary = null;
    private string _selectedProfile = "";
    private UserSelectionModel? _userSelectionModel = null;

    protected override async Task OnInitializedAsync()
    {
        var user = await Client.GetUserAsync();
        _profiles = user.Profiles.Where(x => x.SupportsUserSelectionOptions).ToList();
        _selectedProfileSummary = user.Profiles.Where(x => x.SupportsUserSelectionOptions).FirstOrDefault();
        await SetSelectedProfileAsync(_selectedProfileSummary);

        // Instead of awaiting this async enumerable here, let's capture it in a task
        // and start it in the background. This way, we can await it in the UI.
        _getDocumentsTask = GetDocumentsAsync();
    }
    private async Task OnProfileClickAsync(string selection)
    {
        await SetSelectedProfileAsync(_profiles.FirstOrDefault(x => x.Name == selection));
        await GetDocumentsAsync();
    }

    private async Task SetSelectedProfileAsync(ProfileSummary profile)
    {
        _selectedProfile = profile.Name;
        _selectedProfileSummary = profile;
        _userSelectionModel = await Client.GetProfileUserSelectionModelAsync(profile.Id);
    }

    private bool OnFilter(DocumentSummary document) => document is not null
        && (string.IsNullOrWhiteSpace(_filter) || document.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase));

    private async Task GetDocumentsAsync()
    {
        _isLoadingDocuments = true;

        try
        {
            var documents = await Client.GetCollectionDocumentsAsync(_selectedProfileSummary.Id);
            _documents.Clear();
            foreach (var document in documents)
            {
                _documents.Add(document);
            }
        }
        finally
        {
            _isLoadingDocuments = false;
            StateHasChanged();
        }
    }
    private async Task RefreshAsync()
    {
        await GetDocumentsAsync();
    }
    private async Task SubmitFilesForUploadAsync()
    {
        if (_fileUploads.Any())
        {
            _isUploadingDocuments = true;
            _isIndexingDocuments = false;
            //var cookie = await JSRuntime.InvokeAsync<string>("getCookie", "XSRF-TOKEN");

            var metadata = new Dictionary<string, string>();
            foreach (var option in _userSelectionModel.Options)
            {
                if (!string.IsNullOrEmpty(option.SelectedValue))
                {
                    metadata.Add(option.Name, option.SelectedValue);
                }  
            }
            var result = await Client.UploadDocumentsAsync(_fileUploads.ToArray(), MaxIndividualFileSize, _selectedProfileSummary.Id, metadata);

            Logger.LogInformation("Result: {x}", result);

            if (result.IsSuccessful)
            {
                SnackBarMessage($"Uploaded {result.UploadedFiles.Length} documents.");
                _fileUploads.Clear();
            }
            else
            {
                SnackBarError($"Failed to upload {_fileUploads.Count} documents. {result.Error}");
                _isUploadingDocuments = false;
                _isIndexingDocuments = false;
                await GetDocumentsAsync();
                return;
            }

            _isUploadingDocuments = false;
            _isIndexingDocuments = true;
            StateHasChanged();

            // tried to get the access token and pass it into the API call but it crashes and burns here...
            //var accessToken = await GetAuthMeFieldAsync("access_token");
            //var indexRequest = new DocumentIndexRequest(result, accessToken);
            //var indexResult = await Client.NativeIndexDocumentsAsync(indexRequest);

            //var indexResult = await Client.NativeIndexDocumentsAsync(result);
            //if (indexResult.AllFilesIndexed)
            //{
            //    SnackBarMessage($"{indexResult.IndexedCount} files indexed!");
            //}
            //else
            //{
            //    SnackBarError($"Trigger Index Failure!  Indexed {indexResult.IndexedCount} documents out of {indexResult.DocumentCount}. {indexResult.ErrorMessage}");
            //}
        }
        _isUploadingDocuments = false;
        _isIndexingDocuments = false;
        await GetDocumentsAsync();
    }
    private void SnackBarMessage(string? message) { SnackBarAdd(false, message); }
    private void SnackBarError(string? message) { SnackBarAdd(true, message); }
    private void SnackBarAdd(bool isError, string? message)
    {
        Snackbar.Add(
            message ?? "Error occurred!",
            isError ? Severity.Error : Severity.Success,
            static options =>
            {
                options.ShowCloseIcon = true;
                options.VisibleStateDuration = 10_000;
            });
    }

    private IList<IBrowserFile> _fileUploads = new List<IBrowserFile>();
    private void UploadFiles(IReadOnlyList<IBrowserFile> files)
    {
        foreach (var file in files)
        {
            _fileUploads.Add(file);
        }
    }

    private void OnShowDocumentAsync(DocumentSummary document)
    {
    }


    public void Dispose() => _cancellationTokenSource.Cancel();
}
