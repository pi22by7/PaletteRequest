using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PaletteRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PaletteRequest
{
    internal sealed partial class PaletteRequestPage : ListPage
    {
        private readonly List<ListItem> _items = new();
        private static readonly List<SavedRequest> _history = new();

        public PaletteRequestPage()
        {
            Icon = new IconInfo("\uE8A7"); // Web icon
            Title = "PaletteRequest";
            Name = "API Tester";

            InitializeItems();
        }

        private void InitializeItems()
        {
            _items.Clear();

            // Add commands for different HTTP methods
            _items.AddRange(new[]
            {
                new ListItem(new NewRequestCommand(this, "GET")) { Title = "New GET Request" },
                new ListItem(new NewRequestCommand(this, "POST")) { Title = "New POST Request" },
                new ListItem(new NewRequestCommand(this, "PUT")) { Title = "New PUT Request" },
                new ListItem(new NewRequestCommand(this, "DELETE")) { Title = "New DELETE Request" }
            });

            // Add recent requests
            if (_history.Count > 0)
            {
                _items.Add(new ListItem(new NoOpCommand()) { Title = "Recent Requests", Section = "History" });

                foreach (var request in _history.Take(5))
                {
                    _items.Add(new ListItem(new RepeatRequestCommand(this, request))
                    {
                        Title = $"{request.Method} {request.Url}",
                        Subtitle = $"Last called: {request.LastUsed:g}",
                        Section = "History"
                    });
                }
            }
        }

        public override IListItem[] GetItems()
        {
            return _items.ToArray();
        }

        public void AddToHistory(SavedRequest request)
        {
            var existingIndex = _history.FindIndex(r => r.Url == request.Url && r.Method == request.Method);
            if (existingIndex >= 0)
            {
                _history.RemoveAt(existingIndex);
            }

            _history.Insert(0, request);
            InitializeItems();
            RaiseItemsChanged();
        }
    }
}

internal sealed partial class NewRequestCommand : InvokableCommand
{
    private readonly PaletteRequestPage _page;
    private readonly string _method;

    public NewRequestCommand(PaletteRequestPage page, string method)
    {
        _page = page;
        _method = method;
        Name = $"New {method} Request";
        Icon = new IconInfo("\uE8A7"); // Web icon
    }

    public override CommandResult Invoke()
    {
        var formContent = new RequestFormContent(_page, _method);
        var formPage = new FormContentPage(formContent)
        {
            Title = $"Create {_method} Request"
        };

        return CommandResult.GoToPage(new GoToPageArgs { PageId = formPage.Id, NavigationMode = NavigationMode.Push });
    }
}

internal sealed partial class RepeatRequestCommand : InvokableCommand
{
    private readonly PaletteRequestPage _page;
    private readonly SavedRequest _request;

    public RepeatRequestCommand(PaletteRequestPage page, SavedRequest request)
    {
        _page = page;
        _request = request;
        Name = $"Repeat {request.Method} Request";
        Icon = new IconInfo("\uE72C"); // Refresh icon
    }

    public override CommandResult Invoke()
    {
        _request.LastUsed = DateTime.Now;
        _page.AddToHistory(_request);

        Task.Run(async () =>
        {
            var response = await HttpRequestExecutor.ExecuteRequestAsync(_request);
            string message = response.IsSuccess
                ? $"{response.StatusCode} {response.StatusMessage}"
                : $"Error: {response.ErrorMessage}";

            ToastStatusMessage toast = new ToastStatusMessage($"{_request.Method} {_request.Url}: {message}");
            toast.Show();
        });

        return CommandResult.KeepOpen();
    }
}


internal sealed partial class FormContentPage : ContentPage
{
    private readonly IContent _formContent;

    public FormContentPage(IContent formContent)
    {
        _formContent = formContent;
        Title = "Request Form";
    }

    public override IContent[] GetContent()
    {
        return new IContent[] { _formContent };
    }
}

internal sealed partial class RequestFormContent : FormContent
{
    private readonly PaletteRequestPage _page;
    private readonly string _method;

    public RequestFormContent(PaletteRequestPage page, string method)
    {
        _page = page;
        _method = method;
        TemplateJson = CreateFormTemplate(method);
    }

    private static string CreateFormTemplate(string method)
    {
        var templateBase = @"{
            ""type"": ""AdaptiveCard"",
            ""version"": ""1.0"",
            ""body"": [
                {
                    ""type"": ""TextBlock"",
                    ""text"": ""Create " + method + @" Request"",
                    ""weight"": ""Bolder"",
                    ""size"": ""Medium""
                },
                {
                    ""type"": ""Input.Text"",
                    ""id"": ""url"",
                    ""label"": ""URL"",
                    ""placeholder"": ""https://api.example.com/endpoint""
                },
                {
                    ""type"": ""Input.Text"",
                    ""id"": ""headers"",
                    ""label"": ""Headers (JSON format, optional)"",
                    ""placeholder"": ""{\""Content-Type\"":\""application/json\""}"",
                    ""isMultiline"": true
                }";

        if (method == "POST" || method == "PUT")
        {
            templateBase += @",
                {
                    ""type"": ""Input.Text"",
                    ""id"": ""body"",
                    ""label"": ""Request Body"",
                    ""placeholder"": ""{\""key\"":\""value\""}"",
                    ""isMultiline"": true
                }";
        }

        templateBase += @"
            ],
            ""actions"": [
                {
                    ""type"": ""Action.Submit"",
                    ""title"": ""Send Request""
                }
            ]
        }";

        return templateBase;
    }

    public override CommandResult SubmitForm(string payload)
    {
        try
        {
            var formInput = JsonNode.Parse(payload)?.AsObject();
            if (formInput == null)
            {
                return CommandResult.ShowToast("Error parsing form data");
            }

            var url = formInput["url"]?.GetValue<string>();
            if (string.IsNullOrEmpty(url))
            {
                return CommandResult.ShowToast("URL is required");
            }

            var headersJson = formInput["headers"]?.GetValue<string>();
            var body = formInput["body"]?.GetValue<string>();

            var request = new SavedRequest
            {
                Method = _method,
                Url = url,
                Headers = headersJson ?? "",
                Body = body ?? "",
                LastUsed = DateTime.Now
            };

            _page.AddToHistory(request);

            Task.Run(async () =>
            {
                var response = await HttpRequestExecutor.ExecuteRequestAsync(request);
                string message = response.IsSuccess
                    ? $"{response.StatusCode} {response.StatusMessage}"
                    : $"Error: {response.ErrorMessage}";

                ToastStatusMessage toast = new ToastStatusMessage($"{_method} {url}: {message}");
                toast.Show();
            });

            return CommandResult.GoBack();
        }
        catch (Exception ex)
        {
            return CommandResult.ShowToast($"Error: {ex.Message}");
        }
    }
}

internal static class HttpRequestExecutor
{
    private static readonly HttpClient _httpClient = new();

    public static async Task<HttpResponseInfo> ExecuteRequestAsync(SavedRequest request)
    {
        try
        {
            var httpRequest = new HttpRequestMessage
            {
                Method = new HttpMethod(request.Method),
                RequestUri = new Uri(request.Url)
            };

            if (!string.IsNullOrEmpty(request.Headers))
            {
                var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(request.Headers);
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }

            if ((request.Method == "POST" || request.Method == "PUT") && !string.IsNullOrEmpty(request.Body))
            {
                httpRequest.Content = new StringContent(request.Body, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(httpRequest);
            var statusCode = (int)response.StatusCode;
            var responseBody = await response.Content.ReadAsStringAsync();

            var responseInfo = new HttpResponseInfo
            {
                IsSuccess = true,
                StatusCode = statusCode,
                StatusMessage = response.StatusCode.ToString(),
                Body = responseBody
            };

            foreach (var header in response.Headers)
            {
                responseInfo.Headers.Add(header.Key, string.Join(", ", header.Value));
            }

            foreach (var header in response.Content.Headers)
            {
                responseInfo.Headers.Add(header.Key, string.Join(", ", header.Value));
            }

            return responseInfo;
        }
        catch (Exception ex)
        {
            return new HttpResponseInfo
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

internal class HttpResponseInfo
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string StatusMessage { get; set; } = "";
    public string Body { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
}

internal class SavedRequest
{
    public string Method { get; set; } = "GET";
    public string Url { get; set; } = "";
    public string Headers { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime LastUsed { get; set; } = DateTime.Now;
}

