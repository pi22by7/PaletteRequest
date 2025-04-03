# PaletteRequest

A simple API testing extension for PowerToys Command Palette that allows you to quickly send HTTP requests and view responses without leaving your workflow.

## Features

- Send GET, POST, PUT, and DELETE requests with a few keystrokes
- Add custom headers in JSON format
- Include request bodies for POST and PUT requests
- View formatted response status codes, headers, and body content
- Automatic JSON formatting for JSON responses
- History of your 5 most recent requests for quick repeat access

## Getting Started

### Installation

1. Clone this repository or create a new project using the Command Palette "Create a new extension" command
2. Replace the generated code with the code from this project
3. Build and deploy the extension
4. Restart Command Palette or run the "Reload" command

### Usage

1. Open Command Palette (Win + Alt + Space)
2. Type "API Tester" to find the extension
3. Select the type of request you want to make (GET, POST, PUT, DELETE)
4. Enter the URL and any headers or body data
5. Click "Send Request" to execute
6. View the response details
7. Use the "Repeat Request" command to quickly re-execute previous requests

## Request Options

### URL
Enter the full URL including the protocol (http:// or https://)

### Headers
Enter headers in JSON format. For example:
```json
{
  "Content-Type": "application/json",
  "Authorization": "Bearer your-token-here"
}
```

### Body (for POST and PUT)
Enter the request body. For JSON bodies, format as:
```json
{
  "key": "value",
  "another": 123
}
```

## History

Your 5 most recent requests are saved in the history section for quick access. Selecting a request from history will immediately re-execute it.

## Limitations

- Basic authentication is not directly supported (use Authorization header instead)
- File uploads are not supported
- Request timeouts are not configurable
- Limited response formatting (displayed as plain text)

## Future Enhancements

- Save favorite requests
- Support for file uploads
- Better response formatting for different content types
- Request timeout configuration
- Environment variables for reusable values

## Contributing

Contributions are welcome! Feel free to submit pull requests or open issues for bugs and feature requests.