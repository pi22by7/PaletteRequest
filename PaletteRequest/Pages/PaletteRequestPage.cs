using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PaletteRequest
{
    // Main page for the extension
    internal sealed partial class PaletteRequestPage : ListPage
    {
        private readonly List<ListItem> _items = new();
        private static readonly HttpClient _httpClient = new();
        private readonly List<SavedRequest> _history = new();

        public PaletteRequestPage()
        {
            Icon = new IconInfo("\uE8A7"); // Web icon
            Title = "PaletteRequest";
            Name = "API Tester";

            InitializeItems();
        }

        private void InitializeItems()
        {
            // Create common HTTP method commands
            _items.Add(new ListItem(new NewRequestCommand(this, "GET"))
            {
                Title = "New GET Request",
                Subtitle = "Send a GET request to a URL"
            });

            _items.Add(new ListItem(new NewRequestCommand(this, "POST"))
            {
                Title = "New POST Request",
                Subtitle = "Send a POST request with data to a URL"
            });

            _items.Add(new ListItem(new NewRequestCommand(this, "PUT"))
            {
                Title = "New PUT Request",
                Subtitle = "Send a PUT request with data to a URL"
            });

            _items.Add(new ListItem(new NewRequestCommand(this, "DELETE"))
            {
                Title = "New DELETE Request",
                Subtitle = "Send a DELETE request to a URL"
            });

            // Add history section
            if (_history.Count > 0)
            {
                _items.Add(new ListItem(new NoOpCommand())
                {
                    Title = "Recent Requests",
                    Section = "History"
                });

                // Add recent requests (limited to last 5)
                foreach (var request in _history.GetRange(0, Math.Min(_history.Count, 5)))
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
            // Check if this URL already exists
            var existingIndex = _history.FindIndex(r => r.Url == request.Url && r.Method == request.Method);
            if (existingIndex >= 0)
            {
                _history.RemoveAt(existingIndex);
            }

            // Add to the beginning of history
            _history.Insert(0, request);

            // Rebuild items
            _items.Clear();
            InitializeItems();
            RaiseItemsChanged();
        }
    }

    // Command to create a new request
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
            // Show the request form
            return CommandResult.GoHome();
        }
    }

    // Command to repeat a saved request
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
            // Execute the saved request
            return CommandResult.GoHome();
        }
    }

    // Request execution class
    internal sealed class RequestExecutor
    {
        private static readonly HttpClient _httpClient = new();

        public static async Task<HttpResponseWrapper> ExecuteRequestAsync(SavedRequest request)
        {
            try
            {
                // Create request message
                var httpRequest = new HttpRequestMessage
                {
                    Method = new HttpMethod(request.Method),
                    RequestUri = new Uri(request.Url)
                };

                // Add headers if provided
                if (!string.IsNullOrEmpty(request.Headers))
                {
                    try
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
                    catch
                    {
                        // Ignore header parsing errors
                    }
                }

                // Add body for POST and PUT
                if ((request.Method == "POST" || request.Method == "PUT") && !string.IsNullOrEmpty(request.Body))
                {
                    httpRequest.Content = new StringContent(request.Body, Encoding.UTF8);

                    // Try to set content type if not specified in headers
                    if (!httpRequest.Headers.Contains("Content-Type"))
                    {
                        httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    }
                }

                // Send request
                var response = await _httpClient.SendAsync(httpRequest);
                var statusCode = (int)response.StatusCode;
                var responseBody = await response.Content.ReadAsStringAsync();

                // Create response wrapper
                var responseWrapper = new HttpResponseWrapper
                {
                    IsSuccess = true,
                    StatusCode = statusCode,
                    StatusMessage = response.StatusCode.ToString(),
                    Body = responseBody
                };

                // Add headers
                foreach (var header in response.Headers)
                {
                    responseWrapper.Headers.Add(header.Key, string.Join(", ", header.Value));
                }

                foreach (var header in response.Content.Headers)
                {
                    responseWrapper.Headers.Add(header.Key, string.Join(", ", header.Value));
                }

                return responseWrapper;
            }
            catch (Exception ex)
            {
                return new HttpResponseWrapper
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    // HTTP response wrapper
    internal class HttpResponseWrapper
    {
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; } = "";
        public string Body { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }

    // Class to store saved request information
    internal class SavedRequest
    {
        public string Method { get; set; } = "GET";
        public string Url { get; set; } = "";
        public string Headers { get; set; } = "";
        public string Body { get; set; } = "";
        public DateTime LastUsed { get; set; } = DateTime.Now;
    }
}