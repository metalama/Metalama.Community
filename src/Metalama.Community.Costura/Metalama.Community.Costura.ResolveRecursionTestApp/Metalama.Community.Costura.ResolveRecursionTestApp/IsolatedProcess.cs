// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Metalama.Community.Costura.ResolveRecursionTestApp;

/// <summary>
/// Starts a child process on a window station of its own. Before it calls <c>Environment.FailFast</c>, mscorlib
/// reports the recursive resource lookup with <c>System.Diagnostics.Assert.Fail</c>, which opens a modal dialog when
/// the process runs on an interactive window station. A process that runs elsewhere skips the dialog and dies with
/// the exit code, which is what this test reads.
/// </summary>
internal static class IsolatedProcess
{
    public readonly struct Result
    {
        public Result( uint exitCode, bool timedOut, string? dialogText = null )
        {
            this.ExitCode = exitCode;
            this.TimedOut = timedOut;
            this.DialogText = dialogText;
        }

        public uint ExitCode { get; }

        public bool TimedOut { get; }

        /// <summary>
        /// Gets the text of the windows that the child process left open on its desktop, if it did not exit.
        /// </summary>
        public string? DialogText { get; }
    }

    public static Result Run( string fileName, string arguments, TimeSpan timeout )
    {
        // A window station of its own is the better isolation, because the CLR shows no dialog at all on one that is
        // not visible. Creating one is not allowed to every account, so fall back on a hidden desktop of the current
        // window station: a dialog can then still be opened, but nobody sees it and the wait below times out.
        var windowStation = CreateWindowStation( _desktopName, 0, _winStaAllAccess, IntPtr.Zero );
        var desktop = IntPtr.Zero;
        string? desktopPath = null;

        if ( windowStation != IntPtr.Zero )
        {
            var previousWindowStation = GetProcessWindowStation();

            if ( SetProcessWindowStation( windowStation ) )
            {
                desktop = CreateDesktop( "Default", null, IntPtr.Zero, 0, _desktopAllAccess, IntPtr.Zero );
                SetProcessWindowStation( previousWindowStation );

                if ( desktop != IntPtr.Zero )
                {
                    desktopPath = _desktopName + @"\Default";
                }
            }
        }

        if ( desktop == IntPtr.Zero )
        {
            desktop = CreateDesktop( _desktopName, null, IntPtr.Zero, 0, _desktopAllAccess, IntPtr.Zero );

            if ( desktop != IntPtr.Zero )
            {
                desktopPath = _desktopName;
            }
        }

        try
        {
            var startupInfo = new StartupInfo { Cb = Marshal.SizeOf( typeof(StartupInfo) ) };

            if ( desktopPath != null )
            {
                startupInfo.LpDesktop = desktopPath;
            }
            else
            {
                Console.WriteLine(
                    "Warning: could not create a desktop of its own for the child process. If the reproduction "
                    + $"succeeds, a modal assert dialog opens instead (error {Marshal.GetLastWin32Error()})." );
            }

            startupInfo.DwFlags = _startFUseStdHandles;
            startupInfo.HStdInput = GetStdHandle( _stdInputHandle );
            startupInfo.HStdOutput = GetStdHandle( _stdOutputHandle );
            startupInfo.HStdError = GetStdHandle( _stdErrorHandle );

            var commandLine = new StringBuilder( "\"" + fileName + "\" " + arguments );

            if ( !CreateProcess(
                    null,
                    commandLine,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    true,
                    0,
                    IntPtr.Zero,
                    Path.GetDirectoryName( fileName ),
                    ref startupInfo,
                    out var processInformation ) )
            {
                throw new Win32Exception( Marshal.GetLastWin32Error() );
            }

            try
            {
                var timedOut = WaitForSingleObject( processInformation.Process, (uint) timeout.TotalMilliseconds )
                               != _waitObject0;

                if ( timedOut )
                {
                    var dialogText = desktop == IntPtr.Zero
                        ? null
                        : ReadDesktopWindowText( desktop, processInformation.ProcessId );
                    TerminateProcess( processInformation.Process, 1 );

                    return new Result( 0, true, dialogText );
                }

                GetExitCodeProcess( processInformation.Process, out var exitCode );

                return new Result( exitCode, false );
            }
            finally
            {
                CloseHandle( processInformation.Process );
                CloseHandle( processInformation.Thread );
            }
        }
        finally
        {
            if ( desktop != IntPtr.Zero )
            {
                CloseDesktop( desktop );
            }

            if ( windowStation != IntPtr.Zero )
            {
                CloseWindowStation( windowStation );
            }
        }
    }

    /// <summary>
    /// Reads the text of every window that is open on the given desktop. A child process that does not exit is
    /// waiting on a modal dialog, and this is how the test reports what the dialog said.
    /// </summary>
    private static string? ReadDesktopWindowText( IntPtr desktop, int processId )
    {
        var text = new StringBuilder();

        EnumDesktopWindows(
            desktop,
            ( window, _ ) =>
            {
                // The desktop also hosts invisible windows, such as those of the input method editor, which say
                // nothing about the test.
                GetWindowThreadProcessId( window, out var windowProcessId );

                if ( windowProcessId == processId && IsWindowVisible( window ) )
                {
                    AppendWindowText( window, text );
                    EnumChildWindows( window, ( child, _ ) => AppendWindowText( child, text ), IntPtr.Zero );
                }

                return true;
            },
            IntPtr.Zero );

        return text.Length == 0 ? null : text.ToString();
    }

    private static bool AppendWindowText( IntPtr window, StringBuilder text )
    {
        var buffer = new StringBuilder( 8192 );

        // The child process is blocked in the dialog message loop, so it answers WM_GETTEXT. GetWindowText would
        // return nothing, because it does not cross a process boundary for child windows.
        if ( SendMessageTimeout( window, _wmGetText, buffer.Capacity, buffer, _smtoAbortIfHung, 5000, out _ ) != IntPtr.Zero
             && buffer.Length > 0 )
        {
            text.AppendLine( buffer.ToString() );
        }

        return true;
    }

    private delegate bool EnumWindowsProc( IntPtr window, IntPtr parameter );

    private const string _desktopName = "MetalamaCosturaTest";
    private const uint _wmGetText = 0x000D;
    private const uint _smtoAbortIfHung = 0x0002;
    private const uint _winStaAllAccess = 0x37F;
    private const uint _desktopAllAccess = 0x1FF;
    private const uint _startFUseStdHandles = 0x00000100;
    private const int _stdInputHandle = -10;
    private const int _stdOutputHandle = -11;
    private const int _stdErrorHandle = -12;
    private const uint _waitObject0 = 0;

    [StructLayout( LayoutKind.Sequential, CharSet = CharSet.Unicode )]
    private struct StartupInfo
    {
        public int Cb;
        public string? LpReserved;
        public string? LpDesktop;
        public string? LpTitle;
        public int DwX;
        public int DwY;
        public int DwXSize;
        public int DwYSize;
        public int DwXCountChars;
        public int DwYCountChars;
        public int DwFillAttribute;
        public uint DwFlags;
        public short WShowWindow;
        public short CbReserved2;
        public IntPtr LpReserved2;
        public IntPtr HStdInput;
        public IntPtr HStdOutput;
        public IntPtr HStdError;
    }

    [StructLayout( LayoutKind.Sequential )]
    private struct ProcessInformation
    {
        public IntPtr Process;
        public IntPtr Thread;
        public int ProcessId;
        public int ThreadId;
    }

    [DllImport( "user32.dll", CharSet = CharSet.Unicode, SetLastError = true )]
    private static extern IntPtr CreateWindowStation(
        string name,
        uint flags,
        uint desiredAccess,
        IntPtr securityAttributes );

    [DllImport( "user32.dll", SetLastError = true )]
    private static extern IntPtr GetProcessWindowStation();

    [DllImport( "user32.dll", SetLastError = true )]
    [return: MarshalAs( UnmanagedType.Bool )]
    private static extern bool SetProcessWindowStation( IntPtr windowStation );

    [DllImport( "user32.dll", SetLastError = true )]
    [return: MarshalAs( UnmanagedType.Bool )]
    private static extern bool CloseWindowStation( IntPtr windowStation );

    [DllImport( "user32.dll", CharSet = CharSet.Unicode, SetLastError = true )]
    private static extern IntPtr CreateDesktop(
        string desktop,
        string? device,
        IntPtr deviceMode,
        uint flags,
        uint desiredAccess,
        IntPtr securityAttributes );

    [DllImport( "user32.dll", SetLastError = true )]
    [return: MarshalAs( UnmanagedType.Bool )]
    private static extern bool CloseDesktop( IntPtr desktop );

    [DllImport( "user32.dll" )]
    [return: MarshalAs( UnmanagedType.Bool )]
    private static extern bool IsWindowVisible( IntPtr window );

    [DllImport( "user32.dll", SetLastError = true )]
    private static extern int GetWindowThreadProcessId( IntPtr window, out int processId );

    [DllImport( "user32.dll", SetLastError = true )]
    [return: MarshalAs( UnmanagedType.Bool )]
    private static extern bool EnumDesktopWindows( IntPtr desktop, EnumWindowsProc callback, IntPtr parameter );

    [DllImport( "user32.dll", SetLastError = true )]
    [return: MarshalAs( UnmanagedType.Bool )]
    private static extern bool EnumChildWindows( IntPtr window, EnumWindowsProc callback, IntPtr parameter );

    [DllImport( "user32.dll", CharSet = CharSet.Unicode, SetLastError = true )]
    private static extern IntPtr SendMessageTimeout(
        IntPtr window,
        uint message,
        int wParam,
        StringBuilder lParam,
        uint flags,
        uint timeout,
        out IntPtr result );

    // CreateProcessW parses lpCommandLine in place, so that parameter must be a writable buffer and not a string.
    [DllImport( "kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true )]
    [return: MarshalAs( UnmanagedType.Bool )]
    private static extern bool CreateProcess(
        string? applicationName,
        StringBuilder commandLine,
        IntPtr processAttributes,
        IntPtr threadAttributes,
        [MarshalAs( UnmanagedType.Bool )] bool inheritHandles,
        uint creationFlags,
        IntPtr environment,
        string? currentDirectory,
        ref StartupInfo startupInfo,
        out ProcessInformation processInformation );

    [DllImport( "kernel32.dll", SetLastError = true )]
    private static extern IntPtr GetStdHandle( int stdHandle );

    [DllImport( "kernel32.dll", SetLastError = true )]
    private static extern uint WaitForSingleObject( IntPtr handle, uint milliseconds );

    [DllImport( "kernel32.dll", SetLastError = true )]
    [return: MarshalAs( UnmanagedType.Bool )]
    private static extern bool GetExitCodeProcess( IntPtr process, out uint exitCode );

    [DllImport( "kernel32.dll", SetLastError = true )]
    [return: MarshalAs( UnmanagedType.Bool )]
    private static extern bool TerminateProcess( IntPtr process, uint exitCode );

    [DllImport( "kernel32.dll", SetLastError = true )]
    [return: MarshalAs( UnmanagedType.Bool )]
    private static extern bool CloseHandle( IntPtr handle );
}
