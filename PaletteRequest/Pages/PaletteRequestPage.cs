using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PaletteRequest
{
    // Helper class for logging to a file
    internal static class FileLogger
    {
        private const string LogFilePath = "F:/palette_request_log.txt";

        static FileLogger()
        {
            // Create a new log file or clear existing one on startup
            try
            {
                System.IO.File.WriteAllText(LogFilePath, $"=== PaletteRequest Log Started at {DateTime.Now} ===\r\n");
            }
            catch (Exception ex)
            {
                try
                {
                    System.IO.File.WriteAllText("F:/palette_startup_error.txt", ex.ToString());
                }
                catch
                {
                    // Nothing more we can do
                }
            }
        }

        public static void Log(string message)
        {
            try
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                System.IO.File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // If we can't log to the file, at least try to create a file with the error
                try
                {
                    System.IO.File.WriteAllText("F:/palette_error.txt", ex.ToString());
                }
                catch
                {
                    // Last resort, can't do anything more
                }
            }
        }
    }

    // Main entry point page for the extension
    internal sealed partial class PaletteRequestPage : ListPage
    {
        private readonly List<ListItem> _items = new();
        private static readonly List<SavedRequest> _history = new();

        // Page items for GET, POST, PUT, DELETE requests
        private readonly GetRequestPage _getPage = new GetRequestPage();
        private readonly PostRequestPage _postPage = new PostRequestPage();
        private readonly PutRequestPage _putPage = new PutRequestPage();
        private readonly DeleteRequestPage _deletePage = new DeleteRequestPage();

        public PaletteRequestPage()
        {
            Icon = new IconInfo("\uE8A7"); // Web icon
            Title = "PaletteRequest";
            Name = "API Tester";

            FileLogger.Log("PaletteRequestPage initialized");
            InitializeItems();
        }

        private void InitializeItems()
        {
            FileLogger.Log("Initializing items");
            _items.Clear();

            // Add fixed command items that are direct page objects
            _items.Add(new ListItem(_getPage) { Title = "New GET Request" });
            _items.Add(new ListItem(_postPage) { Title = "New POST Request" });
            _items.Add(new ListItem(_putPage) { Title = "New PUT Request" });
            _items.Add(new ListItem(_deletePage) { Title = "New DELETE Request" });

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

            FileLogger.Log($"Initialized {_items.Count} items");
        }

        public override IListItem[] GetItems()
        {
            FileLogger.Log("GetItems called");
            return _items.ToArray();
        }

        public void AddToHistory(SavedRequest request)
        {
            FileLogger.Log($"Adding request to history: {request.Method} {request.Url}");

            var existingIndex = _history.FindIndex(r => r.Url == request.Url && r.Method == request.Method);
            if (existingIndex >= 0)
            {
                FileLogger.Log("Removing existing entry from history");
                _history.RemoveAt(existingIndex);
            }

            _history.Insert(0, request);
            FileLogger.Log($"History now contains {_history.Count} items");

            InitializeItems();
            FileLogger.Log("InitializeItems completed");

            RaiseItemsChanged();
            FileLogger.Log("RaiseItemsChanged called");
        }
    }

    // Base class for all request pages
    internal abstract class BaseRequestPage : ListPage
    {
        protected readonly string Method;

        protected BaseRequestPage(string method)
        {
            Method = method;
            Icon = new IconInfo("\uE8A7"); // Web icon
            Title = $"New {Method} Request";
            Name = Method;

            // Add an explicit ID for each page
            Id = $"request-page-{method.ToLowerInvariant()}";
            FileLogger.Log($"Created {Method} page with ID: {Id}");
        }

        public override IListItem[] GetItems()
        {
            FileLogger.Log($"GetItems called for {Method} page");

            return new IListItem[]
            {
                new ListItem(new RequestCommand(Method))
                {
                    Title = $"Make a {Method} Request",
                    Subtitle = "Enter request details"
                },
                new ListItem(new OpenURLRequestCommand(Method))
                {
                    Title = $"Test {Method} with httpbin.org",
                    Subtitle = "Sends a test request to httpbin.org"
                },
                new ListItem(new ReturnCommand())
                {
                    Title = "Return to main menu",
                    Subtitle = "Go back to the method selection"
                }
            };
        }
    }

    // Command to go back to the main menu
    internal sealed partial class ReturnCommand : InvokableCommand
    {
        public ReturnCommand()
        {
            Name = "Return";
            Icon = new IconInfo("\uE72B"); // Back icon
        }

        public override CommandResult Invoke()
        {
            FileLogger.Log("ReturnCommand.Invoke called");
            return CommandResult.GoHome();
        }
    }

    // Command to make a request to a test URL
    internal sealed partial class OpenURLRequestCommand : InvokableCommand
    {
        private readonly string _method;

        public OpenURLRequestCommand(string method)
        {
            _method = method;
            Name = $"Test {method}";
            Icon = new IconInfo("\uE8A7"); // Web icon
        }

        public override CommandResult Invoke()
        {
            FileLogger.Log($"OpenURLRequestCommand.Invoke called for {_method}");

            try
            {
                // Define a test URL based on the method
                string url = _method switch
                {
                    "GET" => "https://httpbin.org/get",
                    "POST" => "https://httpbin.org/post",
                    "PUT" => "https://httpbin.org/put",
                    "DELETE" => "https://httpbin.org/delete",
                    _ => "https://httpbin.org"
                };

                FileLogger.Log($"Opening URL in browser: {url}");

                // Most reliable way - use explorer.exe to open URLs
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = url,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                bool started = process.Start();
                FileLogger.Log($"Process.Start explorer.exe result: {started}");

                // Show toast regardless of success to provide feedback
                return CommandResult.ShowToast($"Opening {url} in browser");
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Error in OpenURLRequestCommand.Invoke: {ex}");

                // If the primary method fails, try the most basic approach
                try
                {
                    FileLogger.Log("Attempting to use System.Diagnostics.Process.Start directly");

                    // Try the simplest version as last resort
                    System.Diagnostics.Process.Start("explorer.exe", "https://www.google.com");

                    return CommandResult.ShowToast($"Opened browser with alternative method");
                }
                catch (Exception ex2)
                {
                    FileLogger.Log($"All browser opening methods failed: {ex2.Message}");
                    return CommandResult.ShowToast($"Could not open browser. Check log file.");
                }
            }
        }
    }

    // Concrete request pages for each HTTP method
    internal sealed partial class GetRequestPage : BaseRequestPage
    {
        public GetRequestPage() : base("GET") { }
    }

    internal sealed partial class PostRequestPage : BaseRequestPage
    {
        public PostRequestPage() : base("POST") { }
    }

    internal sealed partial class PutRequestPage : BaseRequestPage
    {
        public PutRequestPage() : base("PUT") { }
    }

    internal sealed partial class DeleteRequestPage : BaseRequestPage
    {
        public DeleteRequestPage() : base("DELETE") { }
    }

    // Command that handles the actual HTTP request form
    internal sealed partial class RequestCommand : InvokableCommand
    {
        private readonly string _method;

        public RequestCommand(string method)
        {
            _method = method;
            Name = $"{method} Request";
            Icon = new IconInfo("\uE8A7"); // Web icon
        }

        public override CommandResult Invoke()
        {
            FileLogger.Log($"RequestCommand.Invoke called for {_method}");

            try
            {
                // Show a simple toast message to confirm clicking worked
                ToastStatusMessage toast = new ToastStatusMessage($"Executing {_method} request...");
                toast.Show();

                // Create a test request with the correct URL for the HTTP method
                string url = _method switch
                {
                    "GET" => "https://httpbin.org/get",
                    "POST" => "https://httpbin.org/post",
                    "PUT" => "https://httpbin.org/put",
                    "DELETE" => "https://httpbin.org/delete",
                    _ => "https://httpbin.org"
                };

                // Create a simple JSON body for POST/PUT
                string body = "";
                if (_method == "POST" || _method == "PUT")
                {
                    body = "{\"name\":\"test\",\"value\":123}";
                }

                // Don't use JSON structure for headers
                string headers = "";

                var request = new SavedRequest
                {
                    Method = _method,
                    Url = url,
                    Headers = headers,
                    Body = body,
                    LastUsed = DateTime.Now
                };

                // Execute the request
                Task.Run(async () =>
                {
                    FileLogger.Log($"Executing test request to {url}");
                    var response = await HttpRequestExecutor.ExecuteRequestAsync(request);
                    string message = response.IsSuccess
                        ? $"{response.StatusCode} {response.StatusMessage}"
                        : $"Error: {response.ErrorMessage}";

                    FileLogger.Log($"Request result: {message}");

                    // Create a more detailed toast with some of the response body
                    string responsePreview = "";
                    if (response.IsSuccess && response.Body.Length > 0)
                    {
                        responsePreview = response.Body.Length > 50
                            ? response.Body.Substring(0, 50) + "..."
                            : response.Body;
                    }

                    string toastMessage = $"{_method} request to {url}: {message}";
                    if (!string.IsNullOrEmpty(responsePreview))
                    {
                        toastMessage += $"\nResponse: {responsePreview}";
                    }

                    ToastStatusMessage resultToast = new ToastStatusMessage(toastMessage);
                    resultToast.Show();
                });

                return CommandResult.Hide();
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Error in RequestCommand.Invoke: {ex}");
                return CommandResult.ShowToast($"Error: {ex.Message}");
            }
        }
    }

    // Command to repeat a previous request
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
            FileLogger.Log($"RepeatRequestCommand.Invoke called for {_request.Method} {_request.Url}");

            _request.LastUsed = DateTime.Now;
            _page.AddToHistory(_request);

            Task.Run(async () =>
            {
                var response = await HttpRequestExecutor.ExecuteRequestAsync(_request);
                string message = response.IsSuccess
                    ? $"{response.StatusCode} {response.StatusMessage}"
                    : $"Error: {response.ErrorMessage}";

                FileLogger.Log($"Request result: {message}");

                ToastStatusMessage toast = new ToastStatusMessage($"{_request.Method} {_request.Url}: {message}");
                toast.Show();
            });

            return CommandResult.KeepOpen();
        }
    }

    // HTTP request executor
    // HTTP request executor
    internal static class HttpRequestExecutor
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static HttpRequestExecutor()
        {
            // Configure HttpClient
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "PaletteRequest/1.0");

            FileLogger.Log("HttpRequestExecutor initialized");
        }

        public static async Task<HttpResponseInfo> ExecuteRequestAsync(SavedRequest request)
        {
            FileLogger.Log($"Executing {request.Method} request to {request.Url}");

            try
            {
                var httpRequest = new HttpRequestMessage
                {
                    Method = new HttpMethod(request.Method),
                    RequestUri = new Uri(request.Url)
                };

                FileLogger.Log("Created HttpRequestMessage");

                // Add headers - manually instead of using JSON deserialization
                if (!string.IsNullOrEmpty(request.Headers))
                {
                    try
                    {
                        // Manual parse of simple JSON headers instead of using JsonSerializer
                        if (request.Headers.Contains("Content-Type"))
                        {
                            string contentType = request.Headers.Split(new[] { "Content-Type" }, StringSplitOptions.None)[1]
                                .Split(new[] { ":" }, StringSplitOptions.None)[1]
                                .Split(new[] { "\"" }, StringSplitOptions.None)[1];

                            httpRequest.Headers.TryAddWithoutValidation("Content-Type", contentType);
                            FileLogger.Log($"Added Content-Type header: {contentType}");
                        }
                    }
                    catch (Exception ex)
                    {
                        FileLogger.Log($"Error parsing headers: {ex.Message}");
                    }
                }

                if ((request.Method == "POST" || request.Method == "PUT") && !string.IsNullOrEmpty(request.Body))
                {
                    httpRequest.Content = new StringContent(request.Body, Encoding.UTF8, "application/json");
                    FileLogger.Log($"Added request body: {request.Body}");
                }

                FileLogger.Log("Sending HTTP request...");
                var response = await _httpClient.SendAsync(httpRequest);
                var statusCode = (int)response.StatusCode;
                FileLogger.Log($"Received response: {statusCode} {response.StatusCode}");

                var responseBody = await response.Content.ReadAsStringAsync();
                FileLogger.Log($"Response body (first 100 chars): {responseBody.Substring(0, Math.Min(responseBody.Length, 100))}");

                var responseInfo = new HttpResponseInfo
                {
                    IsSuccess = true,
                    StatusCode = statusCode,
                    StatusMessage = response.StatusCode.ToString(),
                    Body = responseBody
                };

                foreach (var header in response.Headers)
                {
                    responseInfo.Headers[header.Key] = string.Join(", ", header.Value);
                }

                foreach (var header in response.Content.Headers)
                {
                    responseInfo.Headers[header.Key] = string.Join(", ", header.Value);
                }

                return responseInfo;
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Error executing request: {ex.Message}");
                return new HttpResponseInfo
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    // Response info class
    internal class HttpResponseInfo
    {
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; } = "";
        public string Body { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }

    // Saved request class
    internal class SavedRequest
    {
        public string Method { get; set; } = "GET";
        public string Url { get; set; } = "";
        public string Headers { get; set; } = "";
        public string Body { get; set; } = "";
        public DateTime LastUsed { get; set; } = DateTime.Now;
    }
}