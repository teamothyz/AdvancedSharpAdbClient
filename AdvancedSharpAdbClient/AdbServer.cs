﻿// <copyright file="AdbServer.cs" company="The Android Open Source Project, Ryan Conrad, Quamotion, yungd1plomat, wherewhere">
// Copyright (c) The Android Open Source Project, Ryan Conrad, Quamotion, yungd1plomat, wherewhere. All rights reserved.
// </copyright>

using AdvancedSharpAdbClient.Exceptions;
using System;
using System.Net.Sockets;

namespace AdvancedSharpAdbClient
{
    /// <summary>
    /// <para>Provides methods for interacting with the adb server. The adb server must be running for
    /// the rest of the <c>Managed.Adb</c> library to work.</para>
    /// <para>The adb server is a background process that runs on the host machine.
    /// Its purpose if to sense the USB ports to know when devices are attached/removed,
    /// as well as when emulator instances start/stop. The ADB server is really one
    /// giant multiplexing loop whose purpose is to orchestrate the exchange of data
    /// between clients and devices.</para>
    /// </summary>
    /// <param name="adbClient">The current ADB client that manages the connection.</param>
    /// <param name="adbCommandLineClientFactory">The <see cref="Func{String, IAdbCommandLineClient}"/> to create <see cref="IAdbCommandLineClient"/>.</param>
    public partial class AdbServer(IAdbClient adbClient, Func<string, IAdbCommandLineClient> adbCommandLineClientFactory) : IAdbServer
    {
        /// <summary>
        /// The minimum version of <c>adb.exe</c> that is supported by this library.
        /// </summary>
        public static readonly Version RequiredAdbVersion = new(1, 0, 20);

        /// <summary>
        /// The error code that is returned by the <see cref="SocketException"/> when the connection is refused.
        /// </summary>
        /// <remarks>No connection could be made because the target computer actively refused it.This usually
        /// results from trying to connect to a service that is inactive on the foreign host—that is,
        /// one with no server application running. <seealso href="https://msdn.microsoft.com/en-us/library/ms740668.aspx"/></remarks>
        public const int ConnectionRefused = 10061;

        /// <summary>
        /// The error code that is returned by the <see cref="SocketException"/> when the connection was reset by the peer.
        /// </summary>
        /// <remarks>An existing connection was forcibly closed by the remote host. This normally results if the peer application on the
        /// remote host is suddenly stopped, the host is rebooted, the host or remote network interface is disabled, or the remote
        /// host uses a hard close. This error may also result if a connection was broken due to keep-alive activity detecting
        /// a failure while one or more operations are in progress. <seealso href="https://msdn.microsoft.com/en-us/library/ms740668.aspx"/></remarks>
        public const int ConnectionReset = 10054;

        /// <summary>
        /// <see langword="true"/> if is starting adb server; otherwise, <see langword="false"/>.
        /// </summary>
        protected static bool IsStarting = false;

        /// <summary>
        /// The current ADB client that manages the connection.
        /// </summary>
        protected readonly IAdbClient adbClient = adbClient ?? throw new ArgumentNullException(nameof(adbClient));

        /// <summary>
        /// Gets or sets a function that returns a new instance of a class that implements the
        /// <see cref="IAdbCommandLineClient"/> interface, that can be used to interact with the
        /// <c>adb.exe</c> command line client.
        /// </summary>
        protected readonly Func<string, IAdbCommandLineClient> adbCommandLineClientFactory = adbCommandLineClientFactory ?? throw new ArgumentNullException(nameof(adbCommandLineClientFactory));

        /// <summary>
        /// Initializes a new instance of the <see cref="AdbServer"/> class.
        /// </summary>
        public AdbServer() : this(new AdbClient(), Factories.AdbCommandLineClientFactory)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdbServer"/> class.
        /// </summary>
        /// <param name="adbClient">The current ADB client that manages the connection.</param>
        public AdbServer(IAdbClient adbClient) : this(adbClient, Factories.AdbCommandLineClientFactory)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdbServer"/> class.
        /// </summary>
        /// <param name="adbCommandLineClientFactory">The <see cref="Func{String, IAdbCommandLineClient}"/> to create <see cref="IAdbCommandLineClient"/>.</param>
        public AdbServer(Func<string, IAdbCommandLineClient> adbCommandLineClientFactory) : this(new AdbClient(), adbCommandLineClientFactory)
        {
        }

        /// <summary>
        /// The path to the adb server. Cached from calls to <see cref="StartServer(string, bool)"/>. Used when restarting
        /// the server to figure out where adb is located.
        /// </summary>
        protected static string CachedAdbPath { get; set; }

        /// <summary>
        /// Gets or sets the default instance of the <see cref="IAdbServer"/> interface.
        /// </summary>
        public static IAdbServer Instance { get; set; } = new AdbServer();

        /// <summary>
        /// Throws an error if the path does not point to a valid instance of <c>adb.exe</c>.
        /// </summary>
        protected static Func<string, bool> CheckFileExists { get; set; } = Factories.CheckFileExists;

        /// <inheritdoc/>
        public virtual StartServerResult StartServer(string adbPath, bool restartServerIfNewer = false)
        {
            if (IsStarting) { return StartServerResult.Starting; }
            try
            {
                AdbServerStatus serverStatus = GetStatus();
                Version commandLineVersion = null;

                IAdbCommandLineClient commandLineClient = adbCommandLineClientFactory(adbPath);
                CheckFileExists = commandLineClient.CheckFileExists;

                if (commandLineClient.CheckFileExists(adbPath))
                {
                    CachedAdbPath = adbPath;
                    commandLineVersion = commandLineClient.GetVersion();
                }

                // If the server is running, and no adb path is provided, check if we have the minimum version
                if (adbPath == null)
                {
                    return !serverStatus.IsRunning
                        ? throw new AdbException("The adb server is not running, but no valid path to the adb.exe executable was provided. The adb server cannot be started.")
                        : serverStatus.Version >= RequiredAdbVersion
                        ? StartServerResult.AlreadyRunning
                        : throw new AdbException($"The adb daemon is running an outdated version ${commandLineVersion}, but not valid path to the adb.exe executable was provided. A more recent version of the adb server cannot be started.");
                }

                if (serverStatus.IsRunning)
                {
                    if (serverStatus.Version < RequiredAdbVersion
                        || (restartServerIfNewer && serverStatus.Version < commandLineVersion))
                    {
                        ExceptionExtensions.ThrowIfNull(adbPath);

                        adbClient.KillAdb();
                        commandLineClient.StartServer();
                        return StartServerResult.RestartedOutdatedDaemon;
                    }
                    else
                    {
                        return StartServerResult.AlreadyRunning;
                    }
                }
                else
                {
                    ExceptionExtensions.ThrowIfNull(adbPath);

                    commandLineClient.StartServer();
                    return StartServerResult.Started;
                }
            }
            finally
            {
                IsStarting = false;
            }
        }

        /// <inheritdoc/>
        public virtual StartServerResult RestartServer() => StartServer(CachedAdbPath, true);

        /// <inheritdoc/>
        public virtual StartServerResult RestartServer(string adbPath) =>
            StringExtensions.IsNullOrWhiteSpace(adbPath) ? RestartServer() : StartServer(adbPath, true);

        /// <inheritdoc/>
        public virtual AdbServerStatus GetStatus()
        {
            // Try to connect to a running instance of the adb server
            try
            {
                int versionCode = adbClient.GetAdbVersion();
                return new AdbServerStatus(true, new Version(1, 0, versionCode));
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    return new AdbServerStatus(false, null);
                }
                else
                {
                    // An unexpected exception occurred; re-throw the exception
                    throw;
                }
            }
        }
    }
}
