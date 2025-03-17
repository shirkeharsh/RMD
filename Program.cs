using System;
using System.Net;
using System.Text;
using NAudio.CoreAudioApi;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

class Program
{
    static MMDeviceEnumerator deviceEnumerator;
    static MMDevice device;

    // Import the ShowWindow function from user32.dll to hide the console window
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("kernel32.dll")]
    public static extern IntPtr GetConsoleWindow();

    const int SW_HIDE = 0;

    static void Main(string[] args)
    {
        // Hide the console window
        IntPtr handle = GetConsoleWindow();
        ShowWindow(handle, SW_HIDE); // Hide the console window

        // Initialize the audio device to control the volume
        deviceEnumerator = new MMDeviceEnumerator();
        device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

        // Start a new thread to listen to HTTP requests on all network interfaces and port 5050
        Thread serverThread = new Thread(StartServer);
        serverThread.Start();

        // Keep the server running in the background
        Console.WriteLine("Server is running...");
        Console.WriteLine("Visit http://<your-ip>:5050/ from any device connected to the same Wi-Fi network.");
        Console.ReadLine(); // Keep the console app running
    }

    static void StartServer()
    {
        // Create an HttpListener to handle requests
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://*:5050/"); // Listen on all network interfaces

        try
        {
            listener.Start();

            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                HttpListenerResponse response = context.Response;
                HttpListenerRequest request = context.Request;

                string responseString = "";

                // Serve the webpage with the buttons and current volume
                if (request.Url.AbsolutePath == "/")
                {
                    float currentVolume = device.AudioEndpointVolume.MasterVolumeLevelScalar * 100;

                    responseString = $@"
                    <html>
                    <head>
                        <title>Volume Control</title>
                        <style>
                            body {{
                                font-family: Arial, sans-serif;
                                background-color: #2c3e50;
                                color: #ecf0f1;
                                margin: 0;
                                padding: 0;
                                display: flex;
                                justify-content: center;
                                align-items: center;
                                height: 100vh;
                                flex-direction: column;
                                overflow: hidden;
                            }}

                            h2 {{
                                margin-bottom: 20px;
                                font-size: 24px;
                            }}

                            form {{
                                margin-bottom: 20px;
                            }}

                            button {{
                                background-color: #3498db;
                                color: white;
                                border: none;
                                padding: 10px 20px;
                                font-size: 16px;
                                cursor: pointer;
                                border-radius: 5px;
                                transition: background-color 0.3s ease;
                            }}

                            button:hover {{
                                background-color: #2980b9;
                            }}

                            button:active {{
                                background-color: #1f5f80;
                            }}

                            .button-container {{
                                display: flex;
                                flex-direction: column;
                                align-items: center;
                            }}

                            @media (max-width: 600px) {{
                                h2 {{
                                    font-size: 18px;
                                }}
                                button {{
                                    font-size: 14px;
                                    padding: 8px 16px;
                                }}
                            }}
                        </style>
                    </head>
                    <body>
                        <div class='button-container'>
                            <h2>Current Volume: {currentVolume:F0}%</h2>
                            <form method='POST' action='/increase-volume'>
                                <button type='submit'>Increase Volume</button>
                            </form>
                            <form method='POST' action='/decrease-volume'>
                                <button type='submit'>Decrease Volume</button>
                            </form>
                            <form method='POST' action='/restart'>
                                <button type='submit'>Restart</button>
                            </form>
                            <form method='POST' action='/shutdown'>
                                <button type='submit'>Shutdown</button>
                            </form>
                            <form method='POST' action='/lock'>
                                <button type='submit'>Lock PC</button>
                            </form>
                        </div>
                    </body>
                    </html>";
                }
                else if (request.Url.AbsolutePath == "/increase-volume" && request.HttpMethod == "POST")
                {
                    // Increase the system volume by 10%
                    IncreaseVolume();
                    response.Redirect("/");  // Refresh the page to update volume
                    continue;
                }
                else if (request.Url.AbsolutePath == "/decrease-volume" && request.HttpMethod == "POST")
                {
                    // Decrease the system volume by 10%
                    DecreaseVolume();
                    response.Redirect("/");  // Refresh the page to update volume
                    continue;
                }
                else if (request.Url.AbsolutePath == "/restart" && request.HttpMethod == "POST")
                {
                    // Restart the system
                    RestartSystem();
                    response.Redirect("/");  // Optionally refresh the page after restarting
                    continue;
                }
                else if (request.Url.AbsolutePath == "/shutdown" && request.HttpMethod == "POST")
                {
                    // Shutdown the system
                    ShutdownSystem();
                    response.Redirect("/");  // Optionally refresh the page after shutdown
                    continue;
                }
                else if (request.Url.AbsolutePath == "/lock" && request.HttpMethod == "POST")
                {
                    // Lock the system
                    LockPC();
                    response.Redirect("/");  // Optionally refresh the page after locking
                    continue;
                }

                // Send the response back to the browser
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static void IncreaseVolume()
    {
        // Increase the system volume by 10%
        float newVolume = device.AudioEndpointVolume.MasterVolumeLevelScalar + 0.1f;
        if (newVolume > 1.0f) newVolume = 1.0f; // Ensure volume doesn't exceed 100%
        device.AudioEndpointVolume.MasterVolumeLevelScalar = newVolume;

        Console.WriteLine($"Volume increased to: {newVolume * 100}%");
    }

    static void DecreaseVolume()
    {
        // Decrease the system volume by 10%
        float newVolume = device.AudioEndpointVolume.MasterVolumeLevelScalar - 0.1f;
        if (newVolume < 0.0f) newVolume = 0.0f; // Ensure volume doesn't go below 0%
        device.AudioEndpointVolume.MasterVolumeLevelScalar = newVolume;

        Console.WriteLine($"Volume decreased to: {newVolume * 100}%");
    }

    static void RestartSystem()
    {
        // Restart the system
        Console.WriteLine("System is restarting...");
        Process.Start("shutdown", "/r /f /t 0");  // /r = restart, /f = force close apps, /t 0 = no delay
    }

    static void ShutdownSystem()
    {
        // Shutdown the system
        Console.WriteLine("System is shutting down...");
        Process.Start("shutdown", "/s /f /t 0");  // /s = shutdown, /f = force close apps, /t 0 = no delay
    }

    static void LockPC()
    {
        // Lock the PC
        Console.WriteLine("System is locking...");
        Process.Start("rundll32.exe", "user32.dll,LockWorkStation");  // Lock the workstation
    }
}
